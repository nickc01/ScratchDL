using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    public sealed class DownloadUserStudios : ProgramOption_Base
    {
        public override string Title => "Download User Studios";
        public override string Description => "Downloads all the studios a user has created";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();

            string[] users = Utilities.GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');

            bool dlComments = Utilities.GetCommentDownloadOption();

            async Task DownloadProject(StudioProject project, string username, DirectoryInfo directory)
            {
                Project? projectInfo = await accessor.GetProjectInfo(project.id);
                if (projectInfo == null)
                {
                    Console.WriteLine($"Failed to download project {project.id}. The project may be private");
                }
                else
                {
                    try
                    {
                        DirectoryInfo dir = await accessor.DownloadAndExportProject(project.id, directory);
                        if (dlComments)
                        {
                            await DownloadProjectComments(accessor, username, project.id, dir);
                        }
                        Console.WriteLine("Downloaded Project : " + project.title);
                    }
                    catch (ProjectDownloadException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            foreach (string user in users)
            {
                List<Studio> studios = new();
                List<Task> downloadTasks = new();

                if (string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(Utilities.RemoveIllegalCharacters(user)))
                {
                    continue;
                }

                DirectoryInfo userDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(user));

                if (accessor.ProfileLoginInfo?.user.username == user)
                {
                    await foreach (UserStudio studio in accessor.GetAllStudiosByCurrentUser())
                    {
                        DirectoryInfo studioDirectory = userDirectory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(studio.fields.title));
                        studios.Add(null!);
                        async Task DownloadStudioInfo(UserStudio studio, int index)
                        {
                            Studio? studioInfo = await accessor.GetStudioInfo(studio.id);
                            if (studioInfo == null)
                            {
                                studioInfo = new Studio(
                                    studio.id,
                                    studio.fields.title,
                                    studio.fields.owner.id,
                                    string.Empty,
                                    "visible",
                                    true,
                                    true,
                                    true,
                                    string.Empty,
                                    new Studio.History(studio.fields.datetime_created, studio.fields.datetime_modified));
                            }
                            studios[index] = studioInfo;
                        }

                        downloadTasks.Add(DownloadStudioInfo(studio, studios.Count - 1));

                        List<StudioProject> foundProjects = new();
                        await foreach (StudioProject project in accessor.GetProjectsInStudioByCurrentUser(studio))
                        {
                            Console.WriteLine("Found Project : " + project.title);
                            foundProjects.Add(project);
                            downloadTasks.Add(DownloadProject(project, user, studioDirectory));
                        }

                        await WriteTextToFile(Helpers.PathAddBackslash(studioDirectory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects.OrderBy(p => p.title).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));
                    }
                }
                else
                {
                    await foreach (Studio studio in accessor.GetCuratedStudios(user))
                    {
                        DirectoryInfo studioDirectory = userDirectory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(studio.title));
                        studios.Add(studio);

                        List<StudioProject> foundProjects = new();
                        await foreach (StudioProject project in accessor.GetProjectsInStudio(studio.id))
                        {
                            Console.WriteLine("Found Project : " + project.title);
                            foundProjects.Add(project);
                            downloadTasks.Add(DownloadProject(project, user, studioDirectory));
                        }

                        await WriteTextToFile(Helpers.PathAddBackslash(studioDirectory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects.OrderBy(p => p.title).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));
                    }
                }

                await Task.WhenAll(downloadTasks);
                await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "studios.json", JsonSerializer.Serialize(studios.OrderBy(s => s.title), new JsonSerializerOptions() { WriteIndented = true }));

                Console.WriteLine($"Studios Downloaded : {studios.Count}");
            }


            return false;
        }
    }
}
