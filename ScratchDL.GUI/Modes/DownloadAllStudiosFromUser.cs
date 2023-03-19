using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadAllStudiosFromUser : ProjectDownloadMode
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
            //downloadedProjects = await DownloadDefault(API.GetCuratedStudios(Username), addEntry);
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
                    downloadTasks.Add(ModeUtilities.DownloadProject(data.API, project, null).ContinueWith(project =>
                    {
                        return ModeUtilities.ExportProject(data, project.Result, DownloadComments);
                        //return ExportProject(project.Result, DownloadComments, studioDirectory, null, writeToConsole);
                    }));
                }

                await Helpers.WriteTextToFile(Helpers.PathAddBackslash(studioDirectory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects.OrderBy(p => p.title).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));
            }

            await Task.WhenAll(downloadTasks);
            //await ExportDefault(downloadedProjects, DownloadComments, folderPath, selectedIDs, writeToConsole);
        }
    }

    public class DownloadAllStudiosFromUserUI : DownloadModeUI<DownloadAllStudiosFromUser>
    {
        public DownloadAllStudiosFromUserUI(DownloadAllStudiosFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments), controlsPanel);
        }
    }
}
