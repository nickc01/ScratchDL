using Scratch_Downloader.Options.Base;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class Quit : ProgramOption_Base
    {
        public override string Title => "Quit";
        public override string Description => "Quits the program";

        public override Task<bool> Run(ScratchAPI accessor)
        {
            return Task.FromResult(true);
        }
    }
}
