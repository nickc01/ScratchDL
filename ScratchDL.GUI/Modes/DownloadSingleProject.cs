using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadSingleProject : ProjectDownloadMode
    {
        public DownloadSingleProject(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download a Project";

        public override string Description => "Downloads a project from the Scratch Website";

        public string ProjectURL = string.Empty;

        public bool DownloadComments = true;

        IProject? downloadedProject;

        public override async Task Download(DownloadData data)
        {
            var projectID = ModeUtilities.GetProjectID(ProjectURL);
            if (projectID != null)
            {
                downloadedProject = await ModeUtilities.DownloadProject(await data.API.GetProjectInfo(projectID.Value),data);
            }
        }

        public override Task Export(ExportData data)
        {
            if (downloadedProject != null)
            {
                return ModeUtilities.ExportProject(data, downloadedProject, DownloadComments);
                //await ExportProject(downloadedProject, DownloadComments, folderPath, selectedIDs, writeToConsole);
            }
            return Task.CompletedTask;
        }
    }

    public class DownloadSingleProjectUI : DownloadModeUI<DownloadSingleProject>
    {
        public DownloadSingleProjectUI(DownloadSingleProject modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.ProjectURL), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
