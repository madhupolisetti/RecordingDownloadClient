using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RecordingDownloadClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new Service1() 
            };

            //ApplicationController controller = new ApplicationController();
            //controller.Start();


            if (System.Environment.UserInteractive)
            {
                RunInteractiveServices(ServicesToRun);
            }
            else
            {
                ServiceBase.Run(ServicesToRun);
            }
        }
        static void RunInteractiveServices(ServiceBase[] servicesToRun) {
            Console.WriteLine("Starting Serice In Interactive Mode");
            System.Reflection.MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart", System.Reflection.BindingFlags.Instance| System.Reflection.BindingFlags.NonPublic);
            foreach (ServiceBase serviceToRun in servicesToRun) {
                Console.WriteLine("Starting " + serviceToRun.ServiceName);
                onStartMethod.Invoke(serviceToRun, new object[] { new string[] { } });
                Console.WriteLine("Started");
            }
            Console.WriteLine(System.Environment.NewLine);
            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
            Console.WriteLine("Stopping");
            System.Reflection.MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (ServiceBase serviceToStop in servicesToRun) {
                Console.WriteLine("Stopping " + serviceToStop.ServiceName);
                onStopMethod.Invoke(serviceToStop, null);
                Console.WriteLine("Stopped");
            }
            if (System.Diagnostics.Debugger.IsAttached) {
                Console.WriteLine();
                Console.Write("Press any key to quit");
                Console.ReadKey();
            }
        }
    }
}
