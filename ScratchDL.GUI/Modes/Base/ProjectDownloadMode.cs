using ScratchDL.Enums;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public abstract class ProjectDownloadMode : DownloadMode
    {
        /*class AuthorOverride : IProject
        {
            string _username;
            public IProject Original { get; set; }

            public string Title => Original.Title;

            public long ID => Original.ID;

            public DateTime DateCreated => Original.DateCreated;

            public DateTime DateModified => Original.DateModified;

            public DateTime? DateShared => Original.DateShared;

            public string AuthorUsername => _username;

            public long AuthorID => Original.AuthorID;

            public long Views => Original.Views;

            public long Loves => Original.Loves;

            public long Favorites => Original.Favorites;

            public long Remixes => Original.Remixes;

            public string ThumbnailImage => Original.ThumbnailImage;

            public string Visibility => Original.Visibility;

            public bool IsPublished => Original.IsPublished;

            public AuthorOverride(IProject original, string username)
            {
                Original = original;
                _username = username;
            }
        }

        static Dictionary<long, Task<string?>> cachedUsernames = new Dictionary<long, Task<string?>>();
        private class DownloadedComment
        {
            public Comment? Comment;
            public List<Comment>? Replies;
        }

        protected ProjectDownloadMode(MainWindowViewModel viewModel) : base(viewModel) { }

        protected async Task<IProject> DownloadProject(IProject? project, Action<DownloadEntry>? addEntry)
        {
            if (project == null)
            {
                throw new Exception("Project is private or doesn't exist");
            }
            Debug.WriteLine($"Found Project : {project.Title}");
            string username = project.AuthorUsername;
            if (string.IsNullOrEmpty(project.AuthorUsername))
            {
                if (cachedUsernames.TryGetValue(project.AuthorID, out var result))
                {
                    username = (await result) ?? string.Empty;
                }
                else
                {
                    async Task GetUsernameAndAdd(IProject project)
                    {
                        var projectDLTask = API.GetProjectInfo(project.ID);
                        cachedUsernames.TryAdd(project.AuthorID, GetUsername(projectDLTask));
                        IProject? newProjectInfo = await projectDLTask;
                        if (newProjectInfo == null)
                        {
                            newProjectInfo = new AuthorOverride(project, "UNKNOWN");
                        }
                        addEntry?.Invoke(new DownloadEntry(true, newProjectInfo.ID, newProjectInfo.Title, newProjectInfo.AuthorUsername));
                    }

                    await GetUsernameAndAdd(project);
                    return project;
                }
            }
            addEntry?.Invoke(new DownloadEntry(true, project.ID, project.Title, username));
            return project;
        }

        protected async Task<List<IProject>> DownloadProjectBatch(IAsyncEnumerable<Project> projectDownloader, Action<DownloadEntry>? addEntry, long projectCount = 0)
        {
            cachedUsernames.Clear();
            List<IProject> downloadedProjects = new List<IProject>();
            List<Task> usernameTasks = new List<Task>();
            await foreach (IProject project in projectDownloader)
            {
                Debug.WriteLine($"Found Project : {project.Title}");

                string username = project.AuthorUsername;
                if (string.IsNullOrEmpty(project.AuthorUsername))
                {
                    if (cachedUsernames.TryGetValue(project.AuthorID, out var result))
                    {
                        username = (await result) ?? string.Empty;
                    }
                    else
                    {
                        async Task GetUsernameAndAdd(IProject project)
                        {
                            var projectDLTask = API.GetProjectInfo(project.ID);
                            cachedUsernames.TryAdd(project.AuthorID, GetUsername(projectDLTask));
                            IProject? newProjectInfo = await projectDLTask;
                            if (newProjectInfo == null)
                            {
                                newProjectInfo = new AuthorOverride(project, "UNKNOWN");
                            }
                            downloadedProjects.Add(newProjectInfo);
                            addEntry?.Invoke(new DownloadEntry(true, newProjectInfo.ID, newProjectInfo.Title, newProjectInfo.AuthorUsername));
                            if (projectCount > 0)
                            {
                                SetProgress(100.0 * (downloadedProjects.Count / (double)projectCount));
                            }
                        }

                        usernameTasks.Add(GetUsernameAndAdd(project));
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(project.AuthorUsername))
                {
                    downloadedProjects.Add(new AuthorOverride(project, username));
                }
                else
                {
                    downloadedProjects.Add(project);
                }
                addEntry?.Invoke(new DownloadEntry(true, project.ID, project.Title, username));
                if (projectCount > 0)
                {
                    SetProgress(100.0 * (downloadedProjects.Count / (double)projectCount));
                }
            }
            await Task.WhenAll(usernameTasks);
            return downloadedProjects;
        }

        static async Task<string?> GetUsername(Task<Project?> project)
        {
            return (await project)?.author.username;
        }

        protected static async Task DownloadProjectComments(ScratchAPI api, string username, long project_id, DirectoryInfo directory)
        {
            List<DownloadedComment> downloadedComments = new();

            List<Task<DownloadedComment>> commentDownloads = new();

            await foreach (Comment comment in api.GetProjectComments(username, project_id))
            {
                async Task<DownloadedComment> Download(Comment comment)
                {
                    List<Comment> replies = new();
                    if (comment.reply_count > 0)
                    {
                        await foreach (Comment reply in api.GetRepliesToComment(username, project_id, comment))
                        {
                            replies.Add(reply);
                        }
                    }

                    return new DownloadedComment
                    {
                        Comment = comment,
                        Replies = replies
                    };
                }

                commentDownloads.Add(Download(comment));
            }

            _ = await Task.WhenAll(commentDownloads);

            downloadedComments.AddRange(commentDownloads.Select(t => t.Result));
            if (downloadedComments.Count > 0)
            {
                using (var fileStream = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(directory.FullName) + "comments.json", FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(fileStream))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(downloadedComments, new JsonSerializerOptions() { WriteIndented = true }));
                    }
                }
            }
        }

        protected static long? GetProjectID(string project)
        {
            int index = project.IndexOf("/projects/");
            if (index > -1)
            {
                project = project[(index + 10)..^1];
            }

            if (long.TryParse(project, out long projectID))
            {
                return projectID;
            }
            return null;
        }

        protected async Task ExportProject(IProject project, bool downloadComments, DirectoryInfo folderPath, IEnumerable<long>? selectedIDs, Action<string> writeToConsole)
        {
            if (selectedIDs != null && selectedIDs.Contains(project.ID))
            {
                return;
            }

            try
            {
                DirectoryInfo dir = await API.DownloadAndExportProject(project.ID, folderPath);
                if (downloadComments)
                {
                    await DownloadProjectComments(API, project.AuthorUsername, project.ID, dir);
                }
                writeToConsole($"✔️ Finished : {project.Title}");
            }
            catch (ProjectDownloadException e)
            {
                Debug.WriteLine(e);
                writeToConsole($"❌ Failed to download {project.Title}");
            }
        }

        protected async Task ExportProjectBatch(List<IProject> downloadedProjects, bool downloadComments, DirectoryInfo folderPath, IEnumerable<long>? selectedIDs, Action<string> writeToConsole)
        {
            IProject[] projectsToExport;
            if (selectedIDs != null)
            {
                projectsToExport = downloadedProjects.IntersectBy(selectedIDs, p => p.ID).ToArray();
            }
            else
            {
                projectsToExport = downloadedProjects.ToArray();
            }

            int projectsExported = 0;
            List<Task> exportTasks = new List<Task>();

            foreach (var project in projectsToExport)
            {

                async Task DownloadProject(IProject project)
                {
                    try
                    {
                        DirectoryInfo dir = await API.DownloadAndExportProject(project.ID, folderPath);
                        if (downloadComments)
                        {
                            await DownloadProjectComments(API, project.AuthorUsername, project.ID, dir);
                        }
                        var value = Interlocked.Increment(ref projectsExported);
                        writeToConsole($"✔️ Finished : {project.Title}");
                        SetProgress(100.0 * (value / (double)projectsToExport.Length));
                    }
                    catch (ProjectDownloadException e)
                    {
                        Debug.WriteLine(e);
                        writeToConsole($"❌ Failed to download {project.Title}");
                    }
                }
                exportTasks.Add(DownloadProject(project));
            }

            await Task.WhenAll(exportTasks);
            Debug.WriteLine($"Done Exporting - {projectsExported} projects exported");
        }*/
        protected ProjectDownloadMode(MainWindowViewModel viewModel) : base(viewModel)
        {
        }
    }
}
