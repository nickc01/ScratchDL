using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadUserStudios : ProgramOption_Base
    {
        public override string Title => "Download User Studios";
        public override string Description => "Downloads all the studios a user has created";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            var directory = Utilities.GetDirectory();

            var users = Utilities.GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');

            var dlComments = Utilities.GetCommentDownloadOption();

            async Task DownloadProject(StudioProject project, string username)
            {
                var projectInfo = await accessor.GetProjectInfo(project.id);
                if (projectInfo == null)
                {
                    projectInfo = new Project(project);
                }
                var dir = await accessor.DownloadAndExportProject(projectInfo, directory);
                if (dir != null && dlComments)
                {
                    await DownloadProjectComments(accessor, username, project.id, dir);
                }
            }

            foreach (var user in users)
            {
                List<Studio> studios = new List<Studio>();
                List<Task> downloadTasks = new List<Task>();

                if (string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(user))
                {
                    continue;
                }

                var userDirectory = directory.CreateSubdirectory(user);

                if (accessor.ProfileLoginInfo?.user.username == user)
                {
                    await foreach (var studio in accessor.GetAllStudiosByCurrentUser())
                    {
                        studios.Add(null!);
                        async Task DownloadStudioInfo(UserStudio studio,int index)
                        {
                            var studioInfo = await accessor.GetStudioInfo(studio.id);
                            if (studioInfo == null)
                            {
                                studioInfo = new Studio(studio);
                            }
                            studios[index] = studioInfo;
                        }

                        downloadTasks.Add(DownloadStudioInfo(studio,studios.Count - 1));

                        await foreach (var project in accessor.GetProjectsInStudioByCurrentUser(studio))
                        {
                            downloadTasks.Add(DownloadProject(project,user));
                        }
                    }
                }
                else
                {
                    await foreach (var studio in accessor.GetCuratedStudios(user))
                    {
                        await foreach (var project in accessor.GetProjectsInStudio(studio.id))
                        {
                            downloadTasks.Add(DownloadProject(project, user));
                            studios.Add(studio);
                        }
                    }
                }

                await Task.WhenAll(downloadTasks);
                await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "studios.json", JsonConvert.SerializeObject(studios, Formatting.Indented));

            }

            Console.WriteLine("Done downloading studios!");

            return false;
        }
    }
}
