using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace RecordingDownloadClient
{
    class ApplicationController
    {
        SqlConnection sqlCon = null, updateSqlCon = null;  
        SqlCommand sqlCmd = null,updateSqlCmd = null;
        SqlDataAdapter da = null;
        DataSet ds = null;
        private bool isFetchingValues = false, isProcessing = false,isFetchingValuesS=false;
        public Queue<RecordedClips> clipPathQueue = new Queue<RecordedClips>();
        int lastClipPathId = 0;
        string clipName = "", recordedClipPath = "", clipSavingPath = "";
        private System.Threading.Thread deQueueThread = null;
        private System.Threading.Thread dbPollerThread = null;
        private System.Threading.Thread dbPollerThreadStaging = null;  
        public ApplicationController()
        {
            SharedClass.Logger.Info("Service has started ");
            Console.WriteLine("Service Started");
        }

        public void Start()
        {
            dbPollerThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Pooling));
            dbPollerThread.Name = "PoolingThread";
            dbPollerThread.Start(Environment.PRODUCTION);
            
            if(SharedClass.PollStaging)
            {
                dbPollerThreadStaging = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Pooling));
                dbPollerThreadStaging.Name = "PoolingThreadStaging";
                dbPollerThreadStaging.Start(Environment.STAGING);
            }
            
            SharedClass.Logger.Info("dbPollerThread has been started");
            deQueueThread = new System.Threading.Thread(new System.Threading.ThreadStart(DeQueue));
            deQueueThread.Name = "ProcessingThread";
            deQueueThread.Start();
            SharedClass.Logger.Info("deQueueThread has been started");
        }
        public void Pooling(object input)
        {
            Environment environment = (Environment)input;
            sqlCon = new SqlConnection(SharedClass.GetConnectionString(environment));
            sqlCmd = new SqlCommand("GetPendingRecordedCallsToDownload", sqlCon);
            while (!SharedClass.HasStopSignal)
            {
                try
                {
                    if (environment == Environment.STAGING)
                        isFetchingValuesS = true;
                    else
                        isFetchingValues = true;
                    sqlCmd.Parameters.Clear();
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.Parameters.Add("@LastId", SqlDbType.Int).Value = Slno.GetLastSlno(environment);
                    da = new SqlDataAdapter(sqlCmd);
                    ds = new DataSet();
                    da.Fill(ds);
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        try
                        {
                            foreach (DataRow dr in ds.Tables[0].Rows)
                            {
                                RecordedClips recordedClipObj = new RecordedClips();
                                recordedClipObj.Id = Convert.ToInt32(dr["Id"]);
                                recordedClipObj.Path = dr["RecordingUrl"].ToString();
                                recordedClipObj.Environment = environment;
                                Slno.SetLastSlno(Convert.ToInt32(dr["Id"]), environment);
                                EnQueue(recordedClipObj);
                            }
                        }
                        catch (Exception ex)
                        {
                            SharedClass.Logger.Error("Error in enqueuing the object ==>" + ex.ToString());
                        }

                    }
                }
                catch(Exception ex)
                {
                    SharedClass.Logger.Error("Exception in Getting pending records Reason :" + ex.ToString()); 
                }

                if (environment == Environment.STAGING)
                    isFetchingValuesS = false;
                else
                    isFetchingValues = false;
            }
        }

       

        void EnQueue(RecordedClips clipPathsToBeEnque)
        {
            try
            {
                lock (clipPathQueue)
                {
                    clipPathQueue.Enqueue(clipPathsToBeEnque);
                }
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error("Error in EnQueueRecordedPaths ==>" + ex.ToString());
            }
        }

        void DeQueue()
        {
            while (!SharedClass.HasStopSignal)
            {
                if (clipPathQueue.Count == 0)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(5000);
                    }
                    catch (System.Threading.ThreadAbortException) { }
                    catch (System.Threading.ThreadInterruptedException) { }

                }
                else
                {
                    RecordedClips recordedClipObj = clipPathQueue.Dequeue();
                    isProcessing = true;
                    SaveClip(recordedClipObj);
                    isProcessing = false;
                }
            }

        }

        void SaveClip(RecordedClips recordedClipObj)
        {
            try
            {
                Console.WriteLine("Processing the RecordedClip of id: " + recordedClipObj.Id);
                SharedClass.Logger.Info("Processing the RecordedClip of id: " + recordedClipObj.Id);
                clipName = recordedClipObj.Path.Substring(recordedClipObj.Path.LastIndexOf('/') + 1);

                if(recordedClipObj.Environment == Environment.PRODUCTION)
                    clipSavingPath = SharedClass.SavingPathProduction + clipName;
                else
                    clipSavingPath = SharedClass.SavingPathStaging + clipName;

                System.Net.WebClient client = new System.Net.WebClient();
                client.DownloadFile(recordedClipObj.Path, clipSavingPath);
                Console.WriteLine("Clip has been saved with the name : "+clipName);
                SharedClass.Logger.Info("Clip has been saved with the name : " + clipName);
                UpdateStatus(recordedClipObj.Id,recordedClipObj.Environment);
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error("Exception in SaveClip method ==> " + ex.ToString());
            }


        }
        void UpdateStatus(int Id, Environment environment)
        {
            try
            {
                updateSqlCon = new SqlConnection(SharedClass.GetConnectionString(environment));
                updateSqlCmd = new SqlCommand("UpdateRecordingCallsStatus", updateSqlCon);
                updateSqlCmd.CommandType = CommandType.StoredProcedure;
                updateSqlCmd.Parameters.Add("@Id", SqlDbType.Int).Value = Id;
                updateSqlCmd.Parameters.Add("@Success", SqlDbType.Bit).Direction = ParameterDirection.Output;
                if(updateSqlCon.State != ConnectionState.Open)
                    updateSqlCon.Open();
                updateSqlCmd.ExecuteNonQuery();
                updateSqlCon.Close();
                if (!Convert.ToBoolean(updateSqlCmd.Parameters["@Success"].Value))
                    SharedClass.Logger.Error("Error in updating status and the id is ==> " + Id + "  Environment :"+environment.ToString() );
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error("Error in UpdateStatusmethod ==> " + ex.ToString());
            }
            finally
            {
                try { updateSqlCmd.Dispose(); }
                catch (Exception ex)
                {
                    SharedClass.Logger.Error("Error in disposing the SqlCommand is ==> " + ex.ToString());
                }
                finally { updateSqlCmd = null; }
            }
        }

        private void LoadConfig()
        {
            SharedClass.InitiaLizeLogger();
            SharedClass.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString;
            if(System.Configuration.ConfigurationManager.ConnectionStrings["DbConnectionStringStaging"]!=null)
            {
                SharedClass.ConnectionStringStaging = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnectionStringStaging"].ConnectionString;
                SharedClass.PollStaging = true;
            }
        }

        public void Stop()
        {
            SharedClass.HasStopSignal = true;
            while (isFetchingValues)
            {
                SharedClass.Logger.Info("dbPollerThread is waiting for thread termination : " + dbPollerThread.ThreadState.ToString());
                if (dbPollerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    try
                    {
                        dbPollerThread.Interrupt();
                    }
                    catch (Exception ex)
                    {
                        SharedClass.Logger.Error("Error in interupting dbPollerThread ==> " + ex.ToString());
                    }
                }
                System.Threading.Thread.Sleep(100);
            }

            if(SharedClass.PollStaging)
            {
                while (isFetchingValuesS)
                {
                    SharedClass.Logger.Info("dbPollerThreadStaging is waiting for thread termination : " + dbPollerThreadStaging.ThreadState.ToString());
                    if (dbPollerThreadStaging.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                    {
                        try
                        {
                            dbPollerThreadStaging.Interrupt();
                        }
                        catch (Exception ex)
                        {
                            SharedClass.Logger.Error("Error in interupting dbPollerThread ==> " + ex.ToString());
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
            
            while (isProcessing)
            {
                SharedClass.Logger.Info("DequeueThread is waiting for thread termination : " + deQueueThread.ThreadState.ToString());
                if (deQueueThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    try
                    {
                        deQueueThread.Interrupt();
                    }
                    catch (Exception ex)
                    {
                        SharedClass.Logger.Error("Error in interupting dequeue thread ==> " + ex.ToString());
                    }

                }
                System.Threading.Thread.Sleep(100);
            }
            SharedClass.Logger.Info("Service has been stopped");
        }

    }
}
