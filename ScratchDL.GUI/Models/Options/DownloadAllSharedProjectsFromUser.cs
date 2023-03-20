using ScratchDL.Enums;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Options
{

    public class DownloadAllSharedProjectsFromUser : ProgramOption
    {
        public DownloadAllSharedProjectsFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Shared Projects From User";

        public override string Description => "Downloads all shared projects made by a certain user";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<IProject> downloadedProjects = new List<IProject>();

        public override async Task Download(DownloadData data)
        {
            downloadedProjects = await OptionUtilities.DownloadProjectBatch(data.API.GetPublishedProjects(Username),data);
        }

        public override async Task Export(ExportData data)
        {
            await OptionUtilities.ExportProjectBatch(data,downloadedProjects,DownloadComments);
            await OptionUtilities.SerializeToFile(Helpers.PathAddBackslash(data.FolderPath.FullName) + "project.json", downloadedProjects.OrderBy(p => p.Title));
        }
    }
}
