using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ScratchDL
{
    internal class RegexObjects
    {
        public static readonly Regex FindSharedProjectCount = new Regex(@"<h4>Shared Projects\s?\((\d*)\)<\/h4>", RegexOptions.Compiled);
        public static readonly Regex FindAllProjectCount = new Regex(@"All Projects \(<span data-content=\""project-count\"">(\d*)<\/span>\)", RegexOptions.Compiled);

    }
}
