using Scratch_Downloader.Options.Base;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{

    public sealed class DownloadProject : ProgramOption_Base
    {
        public override string Title => "Download Project";
        public override string Description => "Downloads a project by ID or URL";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] projects = Utilities.GetStringFromConsole("Enter the project URL or ID to download (or multiple seperated by commas)").Split(',', ' ');
            bool downloadComments = Utilities.GetCommentDownloadOption();

            foreach (string project in projects)
            {
                await DownloadProjectTask(accessor, project, directory, downloadComments);
            }
            return false;
        }

        public static async Task DownloadProjectTask(ScratchAPI accessor, string project, DirectoryInfo directory, bool downloadComments)
        {
            int index = project.IndexOf("/projects/");
            if (index > -1)
            {
                project = project[(index + 10)..^1];
            }

            if (long.TryParse(project, out long projectID))
            {
                Project? projectInfo = await accessor.GetProjectInfo(projectID);
                if (projectInfo == null)
                {
                    Console.WriteLine($"Unable to download project {projectID}, either the project doesn't exist or the project is private");
                    return;
                }
                try
                {
                    DirectoryInfo dir = await accessor.DownloadAndExportProject(projectID, directory);
                    if (downloadComments && projectInfo != null)
                    {
                        await DownloadProjectComments(accessor, projectInfo.author.username, projectID, dir);
                    }

                    Console.WriteLine($"Successfully Downloaded Project : {projectInfo!.title}");
                }
                catch (ProjectDownloadException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid Project ID Entered. Make sure you entered a project URL or a numerical ID");
            }
        }
    }
}
