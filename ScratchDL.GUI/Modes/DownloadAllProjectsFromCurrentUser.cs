using Avalonia.Controls;
using Avalonia.Data;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                Debug.WriteLine("Project = " + project.fields.title);
                downloadedProjects.Add(project);
                addEntry(new ProjectEntry(true,project.id,project.fields.title,project.fields.creator.username));
                setProgress(100.0 * (downloadedProjects.Count / (double)totalCount.Value));
            }
        }

        public override Task Export(IEnumerable<long> selectedIDs)
        {
            return Task.CompletedTask;
        }
    }

    public class DownloadAllProjectsFromCurrentUserUI : DownloadModeUI<DownloadAllProjectsFromCurrentUser>
    {
        public DownloadAllProjectsFromCurrentUserUI(DownloadAllProjectsFromCurrentUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            var commentsCheckbox = CreateCheckbox("download_comments", "Download Comments", ModeObject.DownloadComments, b => ModeObject.DownloadComments = b);
            controlsPanel.Children.Add(commentsCheckbox);
        }
    }
}
