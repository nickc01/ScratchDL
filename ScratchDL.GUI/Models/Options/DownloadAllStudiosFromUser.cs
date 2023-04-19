using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Options
{
    public class DownloadAllStudiosFromUser : ProgramOption
    {
        public DownloadAllStudiosFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Studios From User";

        public override string Description => "Downloads all studios a user has curated";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<IStudio> downloadedStudios = new List<IStudio>();

        public override async Task Download(DownloadData data)
        {
            downloadedStudios.Clear();
            await foreach (Studio studio in data.API.GetCuratedStudios(Username))
            {
                downloadedStudios.Add(studio);
                data.AddEntry(new DownloadEntry(true, studio.id, studio.title, Username));
            }
        }

        public override async Task Export(ExportData data)
        {
            List<Task> downloadTasks = new List<Task>();
            foreach (var studio in downloadedStudios)
            {
                DirectoryInfo studioDirectory = data.FolderPath.CreateSubdirectory(Helpers.RemoveIllegalCharacters(studio.Title));

                List<StudioProject> foundProjects = new();
                await foreach (StudioProject project in data.API.GetProjectsInStudio(studio.ID))
                {
                    foundProjects.Add(project);
                    downloadTasks.Add(OptionUtilities.DownloadProject(data.API, project, null).ContinueWith(project =>
                    {
                        return OptionUtilities.ExportProject(data, project.Result, DownloadComments);
                    }));
                }

                await OptionUtilities.SerializeToFile(Helpers.PathAddBackslash(data.FolderPath.FullName) + "project.json", foundProjects.OrderBy(p => p.title));
            }

            await Task.WhenAll(downloadTasks);
            await OptionUtilities.SerializeToFile(Helpers.PathAddBackslash(data.FolderPath.FullName) + "studios.json", downloadedStudios.OrderBy(p => p.Title));
        }
    }
}
