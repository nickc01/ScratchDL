using Avalonia.Controls;
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

namespace ScratchDL.GUI.Modes
{
    public class DownloadAllSharedProjectsFromUser : DownloadMode
    {
        public DownloadAllSharedProjectsFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Shared Projects From User";

        public override string Description => "Downloads all shared projects made by a certain user";

        public string Username = string.Empty;

        public bool DownloadComments = true;

        List<Project> downloadedProjects = new List<Project>();

        public override async Task Download(ScratchAPI api, Action<ProjectEntry> addEntry, Action<double> setProgress)
        {
            //int counter = 0;
            //List<Task> downloadTasks = new();

            downloadedProjects.Clear();

            await foreach (Project project in api.GetPublishedProjects(Username))
            {
                Debug.WriteLine($"Found Project : {project.title}");
                downloadedProjects.Add(project);
                addEntry(new ProjectEntry(true,project.id,project.title,Username));

                /*async Task Download()
                {
                    try
                    {
                        DirectoryInfo dir = await api.DownloadAndExportProject(project, directory);
                        if (downloadComments)
                        {
                            await DownloadProjectComments(accessor, username, project.id, dir);
                        }
                        Console.WriteLine($"Downloaded Project = {project.title}");
                    }
                    catch (ProjectDownloadException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                downloadTasks.Add(Download());

                counter++;*/
            }

            //await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects, new JsonSerializerOptions() { WriteIndented = true }));


            //await Task.WhenAll(downloadTasks);

            //Console.WriteLine($"{counter} Projects Downloaded");
        }

        public override async Task Export(ScratchAPI api, DirectoryInfo folderPath, IEnumerable<long> selectedIDs, Action<string> writeToConsole, Action<double> setProgress)
        {
            var projectsToExport = downloadedProjects.IntersectBy(selectedIDs, p => p.id).ToArray();

            int projectsExported = 0;
            List<Task> exportTasks = new List<Task>();

            foreach (var project in projectsToExport)
            {

                async Task DownloadProject(Project project)
                {
                    try
                    {
                        DirectoryInfo dir = await api.DownloadAndExportProject(project.id, folderPath);
                        if (DownloadComments)
                        {
                            await DownloadProjectComments(api, api.ProfileLoginInfo!.user.username, project.id, dir);
                        }
                        var value = Interlocked.Increment(ref projectsExported);
                        writeToConsole($"✔️ Finished : {project.title}");
                        setProgress(100.0 * (value / (double)projectsToExport.Length));
                    }
                    catch (ProjectDownloadException e)
                    {
                        Debug.WriteLine(e);
                        writeToConsole($"❌ Failed to download {project.title}");
                    }
                }
                exportTasks.Add(DownloadProject(project));
            }

            await Task.WhenAll(exportTasks);
            Console.WriteLine($"Done Exporting - {projectsExported} projects exported");
        }
    }

    public class DownloadAllSharedProjectsFromUserUI : DownloadModeUI<DownloadAllSharedProjectsFromUser>
    {
        public DownloadAllSharedProjectsFromUserUI(DownloadAllSharedProjectsFromUser modeObject) : base(modeObject) { }

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username),controlsPanel);
            CreateAndAddCheckbox(nameof(ModeObject.DownloadComments),controlsPanel);
        }
    }
}
