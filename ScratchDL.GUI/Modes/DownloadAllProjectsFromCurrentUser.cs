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

    public class DownloadAllProjectsFromCurrentUser : DownloadMode
    {
        public bool DownloadComments = true;

        public DownloadAllProjectsFromCurrentUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Projects From Current User";

        public override string Description => "Downloads all projects from the currently logged in user\n(Make sure you login first)";

        List<GalleryProject> downloadedProjects = new List<GalleryProject>();

        public override async Task Download(ScratchAPI api, Action<ProjectEntry> addEntry, Action<double> setProgress)
        {
            downloadedProjects.Clear();
            if (!api.LoggedIn)
            {
                throw new ScratchDL.LoginException(0, "Login Required");
            }
            var totalCount = await api.GetAllProjectCount();
            if (totalCount == null)
            {
                throw new ScratchDL.LoginException(0, "Login Required");
            }

            await foreach (var project in api.GetAllProjectsByCurrentUser())
            {
                downloadedProjects.Add(project);
                addEntry(new ProjectEntry(true,project.id,project.fields.title,project.fields.creator.username));
                setProgress(100.0 * (downloadedProjects.Count / (double)totalCount.Value));
            }
        }

        public override async Task Export(ScratchAPI api, DirectoryInfo folderPath, IEnumerable<long> selectedIDs, Action<string> writeToConsole, Action<double> setProgress)
        {
            var projectsToExport = downloadedProjects.IntersectBy(selectedIDs, p => p.id).ToArray();

            int projectsExported = 0;
            List<Task> exportTasks = new List<Task>();

            foreach (var project in projectsToExport)
            {

                async Task DownloadProject(GalleryProject project)
                {
                    try
                    {
                        DirectoryInfo dir = await api.DownloadAndExportProject(project.id, folderPath);
                        if (DownloadComments)
                        {
                            await DownloadProjectComments(api, api.ProfileLoginInfo!.user.username, project.id, dir);
                        }
                        var value = Interlocked.Increment(ref projectsExported);
                        writeToConsole($"✔️ Finished : {project.fields.title}");
                        setProgress(100.0 * (value / (double)projectsToExport.Length));
                    }
                    catch (ProjectDownloadException e)
                    {
                        Debug.WriteLine(e);
                        writeToConsole($"❌ Failed to download {project.fields.title}");
                    }
                }
                exportTasks.Add(DownloadProject(project));
            }

            /*foreach (var task in exportTasks)
            {
                var message = await task;
                writeToConsole(message);
            }*/
            await Task.WhenAll(exportTasks);
            Console.WriteLine($"Done Exporting - {projectsExported} projects exported");
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
