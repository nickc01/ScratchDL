using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadAllProjects : ProgramOption_Base
    {
        public override string Title => "Download All Projects (Requires Login)";

        public override string Description => "Downloads all projects from the currently logged in user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            if (!accessor.LoggedIn)
            {
                Console.WriteLine("Please login first");
                return false;
            }

            var directory = Utilities.GetDirectory();

            var selfUserDirectory = directory.CreateSubdirectory(accessor.ProfileLoginInfo!.user.username);

            var downloadComments = Utilities.GetCommentDownloadOption();

            await DownloadAllProjectsCurrentProfile(accessor, selfUserDirectory, downloadComments);
            return false;
        }

        public static async Task DownloadAllProjectsCurrentProfile(ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
            directory = directory.CreateSubdirectory("Projects");
            List<Task> downloadTasks = new List<Task>();

            ConcurrentBag<Project> projects = new ConcurrentBag<Project>();
            await foreach (var project in accessor.GetAllProjectsByCurrentUser())
            {
                async Task DownloadProject(GalleryProject p)
                {
                    Console.WriteLine($"Downloading : {p.fields.title} : {p.id}");
                    var projectInfo = await accessor.GetInfoOfProjectByCurrentUser(p);
                    Console.WriteLine("Project Info = " + projectInfo);
                    if (projectInfo == null)
                    {
                        return;
                    }
                    projects.Add(projectInfo);
                    Console.WriteLine($"Exporting : {p.fields.title} : {p.id}");
                    var dir = await accessor.DownloadAndExportProject(projectInfo, directory);
                    if (dir != null && downloadComments)
                    {
                        Console.WriteLine($"Downloading Comments : {p.fields.title}");
                        await DownloadProjectComments(accessor, accessor.ProfileLoginInfo!.user.username, project.id, dir);
                    }
                    Console.WriteLine($"Finished : {p.fields.title}");
                }
                downloadTasks.Add(DownloadProject(project));
            }

            await Task.WhenAll(downloadTasks);

            await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(projects.OrderBy(p => p.title).ToArray(), Formatting.Indented));

            Console.WriteLine("Done downloading projects!");
        }
    }
}
