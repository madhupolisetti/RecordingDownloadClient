using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingDownloadClient
{
    public static class Slno
    {
        private static long _productionSlno = 0;
        private static long _stagingSlno = 0;
        public static long GetLastSlno(Environment environment)
        {
            if (environment == Environment.STAGING)
                return Slno._stagingSlno;
            else
                return Slno._productionSlno;
        }
        public static void SetLastSlno(long slno, Environment environment)
        {
            if (environment == Environment.STAGING)
                Slno._stagingSlno = slno;
            else
                Slno._productionSlno = slno;
        }
    }
}
