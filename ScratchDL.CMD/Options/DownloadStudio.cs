using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    internal sealed class DownloadStudio : ProgramOption_Base
    {
        public override string Title => "Download Studio";
        public override string Description => "Downloads a studio and all its projects";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] studios = Utilities.GetStringFromConsole("Enter the studio URL or ID to download (or multiple seperated by commas)").Split(',', ' ');
            bool downloadComments = Utilities.GetCommentDownloadOption();

            foreach (string studio in studios)
            {
                await DownloadStudioTask(accessor, studio, directory, downloadComments);
            }
            return false;
        }

        public static async Task DownloadStudioTask(ScratchAPI accessor, string studio, DirectoryInfo directory, bool downloadComments)
        {
            int index = studio.IndexOf("/studios/");
            if (index > -1)
            {
                studio = studio[(index + 9)..^1];
            }
            Console.WriteLine("Studio : " + studio);

            if (long.TryParse(studio, out long studioID))
            {
                Studio? info = await accessor.DownloadStudioInfo(studioID);

                if (info != null)
                {

                    DirectoryInfo studioDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(info.title));

                    List<Task> downloadTasks = new();
                    await foreach (StudioProject project in accessor.GetProjectsInStudio(studioID))
                    {
                        Console.WriteLine("Project : " + project.title);
                        downloadTasks.Add(DownloadProject.DownloadProjectTask(accessor, project.id.ToString(), studioDirectory, downloadComments));
                    }

                    await Task.WhenAll(downloadTasks);

                    await WriteTextToFile(Helpers.PathAddBackslash(studioDirectory.FullName) + $"studio.json", JsonSerializer.Serialize(info, new JsonSerializerOptions() { WriteIndented = true }));

                    Console.WriteLine($"Downloaded Studio : {info.title}");
                }
            }
            else
            {
                Console.WriteLine("Invalid Studio ID Entered. Make sure you entered a studio URL or a numerical ID");
            }
        }
    }
}
