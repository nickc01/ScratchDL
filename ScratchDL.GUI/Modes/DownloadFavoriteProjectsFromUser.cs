using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadFavoriteProjectsFromUser : ProjectDownloadMode
    {
        public DownloadFavoriteProjectsFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download Favorite Projects from User";

        public override string Description => "Downloads all projects a user has favorited";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<IProject> downloadedProjects = new List<IProject>();

        public override async Task Download(DownloadData data)
        {
            downloadedProjects = await ModeUtilities.DownloadProjectBatch(data.API.GetFavoriteProjects(Username),data);
        }

        public override Task Export(ExportData data)
        {
            return ModeUtilities.ExportProjectBatch(data, downloadedProjects, DownloadComments);
            //await ExportProjectBatch(downloadedProjects, DownloadComments, folderPath, selectedIDs, writeToConsole);
        }
    }

    public class DownloadFavoriteProjectsFromUserUI : DownloadModeUI<DownloadFavoriteProjectsFromUser>
    {
        public DownloadFavoriteProjectsFromUserUI(DownloadFavoriteProjectsFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
