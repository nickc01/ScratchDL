using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadStudio : ProgramOption_Base
    {
        public override string Title => "Download Studio";
        public override string Description => "Downloads a studio and all its projects";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            var directory = Utilities.GetDirectory();
            var studios = Utilities.GetStringFromConsole("Enter the studio URL or ID to download (or multiple seperated by commas)").Split(',', ' ');
            var downloadComments = Utilities.GetCommentDownloadOption();

            foreach (var studio in studios)
            {
                await DownloadStudioTask(accessor, studio, directory, downloadComments);
            }
            return false;
        }

        public static async Task DownloadStudioTask(ScratchAPI accessor, string studio, DirectoryInfo directory, bool downloadComments)
        {
            var index = studio.IndexOf("/studios/");
            if (index > -1)
            {
                studio = studio[(index + 10)..^1];
            }

            if (long.TryParse(studio, out var studioID))
            {
                var info = await accessor.DownloadStudioInfo(studioID);

                if (info != null)
                {

                    var studioDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(info.title));

                    List<Task> downloadTasks = new List<Task>();
                    await foreach (var project in accessor.GetProjectsInStudio(studioID))
                    {
                        downloadTasks.Add(DownloadProject.DownloadProjectTask(accessor,project.id.ToString(),studioDirectory,downloadComments));
                    }

                    await Task.WhenAll(downloadTasks);

                    await File.WriteAllTextAsync(Utilities.PathAddBackslash(studioDirectory.FullName) + $"studio.json", JsonConvert.SerializeObject(info, Formatting.Indented));
                }
            }
            else
            {
                Console.WriteLine("Invalid Studio ID Entered. Make sure you entered a studio URL or a numerical ID");
            }
        }
    }
}
