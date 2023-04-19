using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
/*using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadProfileInformationForUser : DownloadMode
    {
        public DownloadProfileInformationForUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Followers From User";

        public override string Description => "Download all followers of a certain user";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<IProject> downloadedProjects = new List<IProject>();

        public override async Task Download(Action<DownloadEntry> addEntry)
        {
            downloadedProjects = await DownloadDefault(API.GetUserInfo(Username), addEntry);
        }

        async IAsyncEnumerable<Project> AddName(IAsyncEnumerable<Project> projects)
        {
            await foreach (var project in projects)
            {
                yield return project with { author = project.author with { username = Username } };
            }
        }

        public override async Task Export(ExportData exportData)
        {
            await ExportDefault(downloadedProjects, DownloadComments, folderPath, selectedIDs, writeToConsole);
        }
    }

    public class DownloadProfileInformationForUserView : DownloadModeView<DownloadProfileInformationForUser>
    {
        public DownloadProfileInformationForUserView(DownloadProfileInformationForUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}*/
