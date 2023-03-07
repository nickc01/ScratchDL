using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Scratch_Downloader.Options;

namespace Scratch_Downloader
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Utilities.StartingArguments = args;

			ScratchAPI accessor = ScratchAPI.Create();

			List<ProgramOption_Base> options = new List<ProgramOption_Base>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(ProgramOption_Base)) && !t.IsAbstract))
                {
					var instance = Activator.CreateInstance(type) as ProgramOption_Base;
                    if (instance != null)
                    {
						options.Add(instance);
					}
				}
            }

			while (await Utilities.PickProgramOption(options).Run(accessor) == false) { }
		}
	}
}
