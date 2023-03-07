using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    public record class Health(
        string version,
        double uptime,

        double[] load,

        Health.SQLInfo sql,
        long timestamp,

        Health.Cache cache
        )
    {
        public record class SSL(
            bool ssl,
            bool destroyed,
            long min,
            long max,
            long numUsed,
            long numFree,
            long pendingAcquires,
            long pendingCreates
        );

        public record class Database(
            SSL primary,
            SSL replica
        );

        public record class SQLInfo(
            Database main,
            Database project_comments,
            Database gallery_comments,
            Database unserprofile_comments
        );

        public record class Cache(
            bool connected,
            bool ready
        );
    }
}
