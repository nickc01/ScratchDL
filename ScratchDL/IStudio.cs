using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL
{
    public interface IStudio
    {
        long ID { get; }
        string Title { get; }
        DateTime DateCreated { get; }
        DateTime DateModified { get; }
        string ThumbnailURL { get; }
    }
}
