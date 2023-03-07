//using Newtonsoft.Json;
using Scratch_Downloader.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadUserProjects : ProgramOption_Base
    {
        public override string Title => "Download User Projects";
        public override string Description => "Downloads all projects from a user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
			var directory = Utilities.GetDirectory();
			var users = Utilities.GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');
			var scanType = Utilities.PickEnumOption<ScanType>("How should the user's projects be scanned?", new string[]
			{
							"Will only retrieve the published projects on the user's profile page",
							"Will do a deep scan for any published AND unpublished projects made by the user"
			});

			var downloadComments = Utilities.GetCommentDownloadOption();

			uint scan_depth = Utilities.DEFAULT_SCAN_DEPTH;

			if (scanType == ScanType.DeepScan)
			{
				scan_depth = Utilities.GetScanDepth();
			}

			foreach (var user in users)
			{
				if (string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(user))
				{
					continue;
				}
				var userDirectory = directory.CreateSubdirectory(user);

				if (scanType == ScanType.DeepScan)
				{
					await DownloadAllProjectsDeepScan(accessor, user, scan_depth, userDirectory, downloadComments);
				}
				else
				{
					await DownloadAllProjectsQuickScan(accessor, user, userDirectory, downloadComments);
				}
			}
			return false;
        }

		public static async Task DownloadAllProjectsQuickScan(ScratchAPI accessor, string username, DirectoryInfo directory, bool downloadComments)
		{
			directory = directory.CreateSubdirectory("Projects");
			int counter = 0;
			List<Task> downloadTasks = new List<Task>();

			List<Project> foundProjects = new List<Project>();

			await foreach (var project in accessor.GetPublishedProjects(username))
			{
				foundProjects.Add(project);

				async Task Download()
				{
					var dir = await accessor.DownloadAndExportProject(project, directory);
					if (dir != null && downloadComments)
					{
						await DownloadProjectComments(accessor, username, project.id, dir);
					}
				}

				downloadTasks.Add(Download());

				counter++;
			}

			//await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(foundProjects, Formatting.Indented));
			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects,new JsonSerializerOptions() { WriteIndented = true}));


			await Task.WhenAll(downloadTasks);

			Console.WriteLine("Done!");
			Console.WriteLine($"{counter} Projects Downloaded");
		}

		public static async Task DownloadAllProjectsDeepScan(ScratchAPI accessor, string username, uint scan_depth, DirectoryInfo directory, bool downloadComments)
		{
			directory = directory.CreateSubdirectory("Projects");
			var userInfo = await accessor.GetUserInfo(username);

			int foundProjects = 0;

			ConcurrentDictionary<long, Project> projects = new ConcurrentDictionary<long, Project>();
			ConcurrentDictionary<string, byte> scannedUserNames = new ConcurrentDictionary<string, byte>();
			ConcurrentQueue<Project> listedProjects = new ConcurrentQueue<Project>();

			List<Task> scanTasks = new List<Task>();

			//ConcurrentBag<string> scannedUserNames = new ConcurrentBag<string>();
			scannedUserNames.TryAdd(username, 0);

			async Task ScanProjects(IAsyncEnumerable<Project> projectsEnum, uint recursionLimit = 0)
			{
				if (recursionLimit < 1)
				{
					recursionLimit = uint.MaxValue;
				}

				uint recursion_counter = 0;

				await foreach (var project in projectsEnum)
				{
					if (project.author.id == userInfo.id && projects.TryAdd(project.id, project))
					{
						Interlocked.Increment(ref foundProjects);
						listedProjects.Enqueue(project);
						Console.WriteLine($"Project {project.title}-{project.id} by {username}-{project.author.id}");
					}
					recursion_counter++;

					if (recursion_counter > recursionLimit)
					{
						break;
					}
				}
			}

			scanTasks.Add(ScanProjects(accessor.GetPublishedProjects(username)));


			/*await foreach (var project in accessor.GetPublishedProjects(username))
            {
				
            }*/

			await foreach (var follower in accessor.GetFollowers(username))
			{
				scannedUserNames.TryAdd(follower.username, 0);
				scanTasks.Add(ScanProjects(accessor.GetFavoriteProjects(follower.username), scan_depth));
			}

			await Task.WhenAll(scanTasks);

			scanTasks.Clear();

			List<Task> studioTasks = new List<Task>();

			var projectsToScan = new List<Project>(listedProjects);

			for (int i = 0; i < projectsToScan.Count; i++)
			{
				int depth = 0;

				await foreach (var studio in accessor.GetProjectStudios(username, projectsToScan[i].id))
				{
					async Task ScanManagers()
					{
						await foreach (var manager in accessor.GetStudioManagers(studio.id))
						{
							if (scannedUserNames.TryAdd(manager.username, 0))
							{
								studioTasks.Add(ScanProjects(accessor.GetFavoriteProjects(manager.username), scan_depth / 4));
							}
						}
					}

					scanTasks.Add(ScanManagers());
					depth++;
					if (depth > scan_depth / 4)
					{
						break;
					}
				}
			}

			await Task.WhenAll(scanTasks);
			await Task.WhenAll(studioTasks);

			scanTasks.Clear();

			projectsToScan.AddRange(listedProjects.Except(projectsToScan));

			for (int i = 0; i < projectsToScan.Count; i++)
			{
				async Task ScanRemixes(Project project, uint innerScanDepth)
				{
					uint depth = 0;
					await foreach (var remix in accessor.GetProjectRemixes(project.id))
					{
						if (scannedUserNames.TryAdd(remix.author.username, 0))
						{
							scanTasks.Add(ScanProjects(accessor.GetFavoriteProjects(remix.author.username), scan_depth));

							depth++;

							if (depth >= innerScanDepth)
							{
								break;
							}
						}
					}
				}

				scanTasks.Add(ScanRemixes(projectsToScan[i], scan_depth / 4));
			}

			int completedDownloads = 0;

			async Task Download(Project project)
			{
				var downloadInfo = project with { author = project.author with { username = username } };
				var dir = await accessor.DownloadAndExportProject(downloadInfo, directory);
				if (dir != null)
				{
					Interlocked.Increment(ref completedDownloads);
					if (downloadComments)
					{
						Console.WriteLine($"{project.title} - Downloading Comments");
						await DownloadProjectComments(accessor, username, project.id, dir);
						Console.WriteLine($"{project.title} - Finished Comments");
					}
				}
			}

			List<Task> downloadTasks = new List<Task>();

			foreach (var project in projectsToScan)
			{
				downloadTasks.Add(Download(project));
			}

			await Task.WhenAll(scanTasks);


			foreach (var project in listedProjects.Except(projectsToScan))
			{
				downloadTasks.Add(Download(project));
			}

			//await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(listedProjects.ToArray(), Formatting.Indented));
			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonSerializer.Serialize(foundProjects, new JsonSerializerOptions() { WriteIndented = true }));

			await Task.WhenAll(downloadTasks);
			Console.WriteLine("Done!");
			Console.WriteLine("Total Projects Found = " + foundProjects);
			Console.WriteLine("Total Projects Downloaded = " + completedDownloads);
			if (foundProjects != completedDownloads)
			{
				Console.WriteLine($"{foundProjects - completedDownloads} Projects have been found to be deleted");
			}
		}
	}
}
