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

namespace ScratchDL.GUI.Options
{

    public class DownloadAllProjectsFromCurrentUser : ProgramOption
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

            downloadedProjects = await OptionUtilities.DownloadProjectBatch(ConvertGalleryProjects(data.API, data.API.GetAllProjectsByCurrentUser()),data, totalCount.Value);
        }

        async IAsyncEnumerable<Project> ConvertGalleryProjects(ScratchAPI API, IAsyncEnumerable<GalleryProject> projects)
        {
            await foreach (var project in projects)
            {
                var newProject = new Project();
                yield return newProject with { id = project.id, title = project.fields.title, author = newProject.author with { username = API.ProfileLoginInfo!.user.username } };
            }
        }

        public override async Task Export(ExportData data)
        {
            await OptionUtilities.ExportProjectBatch(data, downloadedProjects, DownloadComments);
            await OptionUtilities.SerializeToFile(Helpers.PathAddBackslash(data.FolderPath.FullName) + "project.json", downloadedProjects.OrderBy(p => p.Title));
        }


    }
}
