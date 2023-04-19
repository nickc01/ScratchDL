using ScratchDL.CMD.Options.Base;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    internal sealed class Quit : ProgramOption_Base
    {
        public override string Title => "Quit";
        public override string Description => "Quits the program";

        public override Task<bool> Run(ScratchAPI accessor)
        {
            return Task.FromResult(true);
        }
    }
}
