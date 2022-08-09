using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    public class HealthInfo
    {
        public class SSLInfo
        {
            public bool ssl;
            public bool destroyed;
            public long min;
            public long max;
            public long numUsed;
            public long numFree;
            public long pendingAcquires;
            public long pendingCreates;
        }

        public class DatabaseInfo
        {
            public SSLInfo primary;
            public SSLInfo replica;
        }

        public class SQLInfo
        {
            public DatabaseInfo main;
            public DatabaseInfo project_comments;
            public DatabaseInfo gallery_comments;
            public DatabaseInfo unserprofile_comments;
        }

        public class CacheInfo
        {
            public bool connected;
            public bool ready;
        }



        public string version;
        public double uptime;

        public double[] load;

        public SQLInfo sql;
        public long timestamp;

        public CacheInfo cache;


    }
}
