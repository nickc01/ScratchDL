using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    public class CommentInfo
    {
        public class Author
        {
            public long id;
            public string username;
            public bool scratchteam;
            public string image;
        }

        public long id;
        public long? parent_id;
        public long? commentee_id;

        public string content;

        public string datetime_created;
        public string datetime_modified;
        public string visiblity;

        public Author author;

        public long reply_count;
    }

}
