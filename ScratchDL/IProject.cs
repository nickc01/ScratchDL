using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL
{
    public interface IProject
    {
        public string Title { get; }
        public long ID { get; }
        public DateTime DateCreated { get; }
        public DateTime DateModified { get; }
        public DateTime? DateShared { get; }
        public string AuthorUsername { get; }
        public long AuthorID { get; }
        public long Views { get; }
        public long Loves { get; }
        public long Favorites { get; }
        public long Remixes { get; }
        public string ThumbnailImage { get; }
        public string Visibility { get; }
        public bool IsPublished { get; }
    }
}
