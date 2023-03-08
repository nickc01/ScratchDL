using Scratch_Downloader.Options.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Utilities.StartingArguments = args;

            ScratchAPI accessor = ScratchAPI.Create();

            List<ProgramOption_Base> options = new();

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type? type in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(ProgramOption_Base)) && !t.IsAbstract))
                {
                    if (Activator.CreateInstance(type) is ProgramOption_Base instance)
                    {
                        options.Add(instance);
                    }
                }
            }

            while (await Utilities.PickProgramOption(options).Run(accessor) == false) { }
        }
    }
}
