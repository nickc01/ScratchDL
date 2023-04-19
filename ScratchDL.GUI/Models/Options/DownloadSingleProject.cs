using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Options
{
    public class DownloadSingleProject : ProgramOption
    {
        public DownloadSingleProject(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download a Project";

        public override string Description => "Downloads a project from the Scratch Website";

        public string ProjectURL = string.Empty;

        public bool DownloadComments = true;

        IProject? downloadedProject;

        public override async Task Download(DownloadData data)
        {
            var projectID = OptionUtilities.GetProjectID(ProjectURL);
            if (projectID != null)
            {
                var project = await data.API.GetProjectInfo(projectID.Value);
                if (project != null)
                {
                    downloadedProject = await OptionUtilities.DownloadProject(project, data);
                }
            }
        }

        public override Task Export(ExportData data)
        {
            if (downloadedProject != null)
            {
                return OptionUtilities.ExportProject(data, downloadedProject, DownloadComments);
            }
            return Task.CompletedTask;
        }
    }
}
