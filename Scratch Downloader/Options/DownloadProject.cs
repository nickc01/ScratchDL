using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{

    public sealed class DownloadProject : ProgramOption_Base
    {
        public override string Title => "Download Project";
        public override string Description => "Downloads a project by ID or URL";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            var directory = Utilities.GetDirectory();
            var projects = Utilities.GetStringFromConsole("Enter the project URL or ID to download (or multiple seperated by commas)").Split(',', ' ');
            var downloadComments = Utilities.GetCommentDownloadOption();

            foreach (var project in projects)
            {
                await DownloadProjectTask(accessor, project, directory, downloadComments);
            }
            return false;
        }

        public static async Task DownloadProjectTask(ScratchAPI accessor, string project, DirectoryInfo directory, bool downloadComments)
        {
            var index = project.IndexOf("/projects/");
            if (index > -1)
            {
                project = project[(index + 10)..^1];
            }

            if (long.TryParse(project, out var projectID))
            {
                var projectInfo = await accessor.GetProjectInfo(projectID);
                var dir = await accessor.DownloadAndExportProject(projectID, directory);
                if (dir != null && downloadComments && projectInfo != null)
                {
                    await DownloadProjectComments(accessor, projectInfo.author.username, projectID, dir);
                }
            }
            else
            {
                Console.WriteLine("Invalid Project ID Entered. Make sure you entered a project URL or a numerical ID");
            }
        }
    }
}
