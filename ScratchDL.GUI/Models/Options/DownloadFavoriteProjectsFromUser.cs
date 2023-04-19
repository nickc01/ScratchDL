using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Options
{
    public class DownloadFavoriteProjectsFromUser : ProgramOption
    {
        public DownloadFavoriteProjectsFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download Favorite Projects from User";

        public override string Description => "Downloads all projects a user has favorited";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<IProject> downloadedProjects = new List<IProject>();

        public override async Task Download(DownloadData data)
        {
            downloadedProjects = await OptionUtilities.DownloadProjectBatch(data.API.GetFavoriteProjects(Username),data);
        }

        public override async Task Export(ExportData data)
        {
            await OptionUtilities.ExportProjectBatch(data, downloadedProjects, DownloadComments);
            await OptionUtilities.SerializeToFile(Helpers.PathAddBackslash(data.FolderPath.FullName) + "projects.json", downloadedProjects.OrderBy(p => p.Title));
        }
    }
}
