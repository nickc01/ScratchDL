using Avalonia.Controls;
using Avalonia.Data;
using ScratchDL.Enums;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{

    public class DownloadAllProjectsFromCurrentUser : ProjectDownloadMode
    {
        public bool DownloadComments = true;

        public DownloadAllProjectsFromCurrentUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Projects From Current User";

        public override string Description => "Downloads all projects from the currently logged in user\n(Make sure you login first)";

        List<IProject> downloadedProjects = new List<IProject>();

        public override async Task Download(DownloadData data)
        {
            downloadedProjects.Clear();
            if (!data.API.LoggedIn)
            {
                throw new ScratchDL.LoginException(0, "Login Required");
            }
            var totalCount = await data.API.GetAllProjectCount();
            if (totalCount == null)
            {
                throw new ScratchDL.LoginException(0, "Login Required");
            }

            downloadedProjects = await ModeUtilities.DownloadProjectBatch(ConvertGalleryProjects(data.API, data.API.GetAllProjectsByCurrentUser()),data, totalCount.Value);
            //downloadedProjects = await DownloadProjectBatch(ConvertGalleryProjects(API.GetAllProjectsByCurrentUser()),addEntry,totalCount.Value);
            /*await foreach (var project in api.GetAllProjectsByCurrentUser())
            {
                downloadedProjects.Add(project);
                addEntry(new ProjectEntry(true,project.id,project.fields.title,project.fields.creator.username));
                setProgress(100.0 * (downloadedProjects.Count / (double)totalCount.Value));
            }*/
        }

        async IAsyncEnumerable<Project> ConvertGalleryProjects(ScratchAPI API, IAsyncEnumerable<GalleryProject> projects)
        {
            await foreach (var project in projects)
            {
                var newProject = new Project();
                yield return newProject with { id = project.id, title = project.fields.title, author = newProject.author with { username = API.ProfileLoginInfo!.user.username } };
            }
        }

        public override Task Export(ExportData exportData)
        {
            return ModeUtilities.ExportProjectBatch(exportData, downloadedProjects, DownloadComments);
            //await ExportProjectBatch(downloadedProjects, DownloadComments, folderPath, selectedIDs, writeToConsole);
            /*var projectsToExport = downloadedProjects.IntersectBy(selectedIDs, p => p.id).ToArray();

            int projectsExported = 0;
            List<Task> exportTasks = new List<Task>();

            foreach (var project in projectsToExport)
            {

                async Task DownloadProject(Project project)
                {
                    try
                    {
                        DirectoryInfo dir = await api.DownloadAndExportProject(project.id, folderPath);
                        if (DownloadComments)
                        {
                            await DownloadProjectComments(api, api.ProfileLoginInfo!.user.username, project.id, dir);
                        }
                        var value = Interlocked.Increment(ref projectsExported);
                        writeToConsole($"✔️ Finished : {project.title}");
                        setProgress(100.0 * (value / (double)projectsToExport.Length));
                    }
                    catch (ProjectDownloadException e)
                    {
                        Debug.WriteLine(e);
                        writeToConsole($"❌ Failed to download {project.title}");
                    }
                }
                exportTasks.Add(DownloadProject(project));
            }

            await Task.WhenAll(exportTasks);
            Console.WriteLine($"Done Exporting - {projectsExported} projects exported");*/
        }


    }

    public class DownloadAllProjectsFromCurrentUserUI : DownloadModeUI<DownloadAllProjectsFromCurrentUser>
    {
        public DownloadAllProjectsFromCurrentUserUI(DownloadAllProjectsFromCurrentUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
