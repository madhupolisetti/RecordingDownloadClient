using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingDownloadClient
{
    public class RecordedClips
    {
        private int _id = 0;
        private string _path = string.Empty;
        private Environment _environment = Environment.PRODUCTION;
        public int Id
        {
            get { return this._id; }
            set { this._id = value; }
        }
        public string Path
        {
            get { return this._path; }
            set { this._path = value; }
        }
        public Environment Environment
        {
            get { return this._environment; }
            set { this._environment = value; }
        }
    }
}
