using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public interface ILoginable
    {
        Task Login(string username, string password);
    }
}
