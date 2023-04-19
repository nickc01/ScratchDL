using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    public record class News(
        long id,
        string stamp,
        string headline,
        string url,
        string image,
        string copy
    );
}
