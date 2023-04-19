using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    internal sealed class DownloadAllProjects : ProgramOption_Base
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

            DirectoryInfo directory = Utilities.GetDirectory();

            DirectoryInfo selfUserDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(accessor.ProfileLoginInfo!.user.username));

            bool downloadComments = Utilities.GetCommentDownloadOption();

            await DownloadAllProjectsCurrentProfile(accessor, selfUserDirectory, downloadComments);
            return false;
        }

        public static async Task DownloadAllProjectsCurrentProfile(ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
            directory = directory.CreateSubdirectory("Projects");
            List<Task> downloadTasks = new();

            ConcurrentBag<Project> projects = new();
            await foreach (GalleryProject project in accessor.GetAllProjectsByCurrentUser())
            {
                async Task DownloadProject(GalleryProject p)
                {
                    Console.WriteLine($"Downloading : {p.fields.title} : {p.id}");
                    Project? projectInfo = await accessor.GetInfoOfProjectByCurrentUser(p);
                    if (projectInfo == null)
                    {
                        return;
                    }
                    projects.Add(projectInfo);
                    Console.WriteLine($"Exporting : {p.fields.title} : {p.id}");
                    try
                    {
                        DirectoryInfo dir = await accessor.DownloadAndExportProject(projectInfo, directory);
                        if (downloadComments)
                        {
                            Console.WriteLine($"Downloading Comments : {p.fields.title}");
                            await DownloadProjectComments(accessor, accessor.ProfileLoginInfo!.user.username, project.id, dir);
                        }
                        Console.WriteLine($"Finished : {p.fields.title}");
                    }
                    catch (ProjectDownloadException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                downloadTasks.Add(DownloadProject(project));
            }

            await Task.WhenAll(downloadTasks);

            await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "projects.json", JsonSerializer.Serialize(projects.OrderBy(p => p.title).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));

            Console.WriteLine("Done downloading projects!");
        }
    }
}
