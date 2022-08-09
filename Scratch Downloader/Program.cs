using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Scratch_Downloader
{
	public enum ProgramOptions
	{
		Quit,
		BackupEntireScratchUser,
		DownloadEntireGallery,
		DownloadAllProjectsFromUser,
		DownloadFavorites,
		DownloadProject
	}

	public enum ScanType
    {
		QuickScan,
		DeepScan
    }

	public enum DownloadComments
    {
		No,
		Yes
    }


	class Program
	{
		public const int SCAN_DEPTH = 400;

		static string[] arguments;

		static string Prettify(string input)
		{
			StringBuilder builder = new StringBuilder(input);
			if (builder.Length > 0)
			{
				if (char.IsLower(builder[0]))
				{
					builder[0] = char.ToUpper(builder[0]);
				}
				builder.Replace("_", "");

				var output = builder.ToString();

				output = Regex.Replace(output, @"([a-z])([A-Z])", "$1 $2");
				output = Regex.Replace(output, @"([a-zA-Z])([A-Z])([a-z])", "$1 $2$3");

				return output;
			}

			return input;
		}

		static bool HasArgument(int index, out string arg)
		{
			if (index < arguments.Length)
			{
				arg = arguments[index];
				return true;
			}
			else
			{
				arg = "";
				return false;
			}
		}

		static string GetPasswordInput()
		{
			var pass = string.Empty;
			ConsoleKey key;
			do
			{
				var keyInfo = Console.ReadKey(intercept: true);
				key = keyInfo.Key;

				if (key == ConsoleKey.Backspace && pass.Length > 0)
				{
					Console.Write("\b \b");
					pass = pass[0..^1];
				}
				else if (!char.IsControl(keyInfo.KeyChar))
				{
					Console.Write("*");
					pass += keyInfo.KeyChar;
				}
			} while (key != ConsoleKey.Enter);
			return pass;
		}

		static async Task Main(string[] args)
		{
			arguments = args;

			string username = "";
			if (!HasArgument(0,out username))
			{
				Console.WriteLine("Enter username to login to: ");
				username = Console.ReadLine();
			}

			string password = "";
			if (!HasArgument(1,out password))
			{
				Console.WriteLine("Enter password to login to:");
				password = GetPasswordInput();
			}

			Console.WriteLine();
			Console.WriteLine("Logging in...");

			ScratchAPI accessor = null;

			try
			{
				accessor = await ScratchAPI.Create(username, password);
			}
			catch (NoInternetException)
			{
				Console.WriteLine("Failed to login: Make sure you are connected to the internet");
				return;
			}
			catch (LoginException)
			{
				Console.WriteLine("Failed to login: Make sure the credentials are correct");
				return;
			}

			Console.WriteLine("Login Successful!");
			Console.WriteLine();

			while (true)
			{
				PickOption<ProgramOptions>("What do you want to do?", out var option);

                if (option == ProgramOptions.Quit)
                {
					return;
                }

				var directory = GetDirectory();
				DirectoryInfo selfUserDirectory = null;
				uint scan_depth = 0;
				string[] users;
				ScanType scanType;
				DownloadComments downloadComments;
				switch (option)
				{
					case ProgramOptions.DownloadEntireGallery:
						selfUserDirectory = directory.CreateSubdirectory(accessor.ProfileLoginInfo.user.username);

						PickOption("Download Project Comments?", out downloadComments, DownloadComments.Yes);

						await DownloadAllProjectsCurrentProfile(accessor, selfUserDirectory, downloadComments == DownloadComments.Yes);
						break;
					case ProgramOptions.BackupEntireScratchUser:
						users = GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');
						if (!users.Contains(accessor.ProfileLoginInfo.user.username))
						{
							PickOption("How should the user's projects be scanned?", out scanType, new string[]
							{
								"Will only retrieve the published projects on the user's profile page",
								"Will do a deep scan for any published AND unpublished projects made by the user"
							});
						}
						else
                        {
							scanType = ScanType.QuickScan;
                        }

						PickOption("Download Project Comments?", out downloadComments, DownloadComments.Yes);

                        foreach (var user in users)
                        {
							var userDirectory = directory.CreateSubdirectory(user);
							if (user == accessor.ProfileLoginInfo.user.username)
                            {
								await DownloadAllProjectsCurrentProfile(accessor, userDirectory, downloadComments == DownloadComments.Yes);
							}
							else
                            {
								if (scanType == ScanType.DeepScan)
								{
									await DownloadAllProjectsDeepScan(accessor, user, scan_depth, userDirectory, downloadComments == DownloadComments.Yes);
								}
								else
								{
									await DownloadAllProjectsQuickScan(accessor, user, userDirectory, downloadComments == DownloadComments.Yes);
								}
							}
							await DownloadAllStudiosCurrentProfile(accessor, userDirectory, downloadComments == DownloadComments.Yes);
							await DownloadFavoriteProjects(user, accessor, userDirectory, downloadComments == DownloadComments.Yes);
							await DownloadFollowers(user, accessor, userDirectory);
							await DownloadFollowing(user, accessor, userDirectory);
							await DownloadProfileInfo(user, accessor, userDirectory);
						}

						break;
					case ProgramOptions.DownloadAllProjectsFromUser:
						users = GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',',' ');
                        PickOption("How should the user's projects be scanned?", out scanType, new string[]
						{
							"Will only retrieve the published projects on the user's profile page",
							"Will do a deep scan for any published AND unpublished projects made by the user"
						});

                        PickOption("Download Project Comments?", out downloadComments, DownloadComments.Yes);

						if (scanType == ScanType.DeepScan)
                        {
							scan_depth = GetUIntFromConsole("Enter the scan depth. The higher the number, the more projects that could be found, but the longer the scan will take", SCAN_DEPTH);
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
								await DownloadAllProjectsDeepScan(accessor, user, scan_depth, userDirectory, downloadComments == DownloadComments.Yes);
							}
							else
							{
								await DownloadAllProjectsQuickScan(accessor, user, userDirectory, downloadComments == DownloadComments.Yes);
							}
						}
						break;
					case ProgramOptions.DownloadFavorites:
						users = GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');
						PickOption("Download Project Comments?", out downloadComments, DownloadComments.Yes);

						foreach (var user in users)
						{
							var userDirectory = directory.CreateSubdirectory(user);
							await DownloadFavoriteProjects(user, accessor, userDirectory, downloadComments == DownloadComments.Yes);
						}
						break;
					default:
						break;
				}
			}
		}

		/// <summary>
		/// Downloads all projects from the currently logged in user
		/// </summary>
		/// <param name="accessor"></param>
		/// <param name="directory"></param>
		/// <returns></returns>
		static async Task DownloadAllProjectsCurrentProfile(ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
			directory = directory.CreateSubdirectory("Projects");
			List<Task> downloadTasks = new List<Task>();

			List<ProjectInfo> projects = new List<ProjectInfo>();
			await foreach (var project in accessor.GetGalleryProjects())
			{
				async Task DownloadProject(GalleryProjectInfo p)
                {
					var projectInfo = await accessor.GetProjectInfo(p);
					projects.Add(projectInfo);
					var dir = await accessor.DownloadAndExportProject(projectInfo, directory);
                    if (dir != null && downloadComments)
                    {
						await DownloadProjectComments(accessor, accessor.ProfileLoginInfo.user.username, project.pk, dir);
                    }
				}
				downloadTasks.Add(DownloadProject(project));
			}

			await Task.WhenAll(downloadTasks);

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(projects, Formatting.Indented));

			Console.WriteLine("Done downloading projects!");
		}

		static async Task DownloadAllProjectsQuickScan(ScratchAPI accessor, string username, DirectoryInfo directory, bool downloadComments)
        {
			directory = directory.CreateSubdirectory("Projects");
			int counter = 0;
			List<Task> downloadTasks = new List<Task>();

			List<ProjectInfo> foundProjects = new List<ProjectInfo>();

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

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(foundProjects, Formatting.Indented));

			await Task.WhenAll(downloadTasks);

			Console.WriteLine("Done!");
			Console.WriteLine($"{counter} Projects Downloaded");
		}

		static async Task DownloadAllProjectsDeepScan(ScratchAPI accessor, string username, uint scan_depth, DirectoryInfo directory, bool downloadComments)
		{
			directory = directory.CreateSubdirectory("Projects");
			var userInfo = await accessor.GetUserInfo(username);

			int foundProjects = 0;

			ConcurrentDictionary<long, ProjectInfo> projects = new ConcurrentDictionary<long, ProjectInfo>();
			ConcurrentDictionary<string, byte> scannedUserNames = new ConcurrentDictionary<string, byte>();
			ConcurrentQueue<ProjectInfo> listedProjects = new ConcurrentQueue<ProjectInfo>();

			List<Task> scanTasks = new List<Task>();

			//ConcurrentBag<string> scannedUserNames = new ConcurrentBag<string>();
			scannedUserNames.TryAdd(username,0);

			async Task ScanProjects(IAsyncEnumerable<ProjectInfo> projectsEnum, uint recursionLimit = 0)
            {
                if (recursionLimit < 1)
                {
					recursionLimit = uint.MaxValue;
                }

				uint recursion_counter = 0;

                await foreach (var project in projectsEnum)
                {
					if (project.author.id == userInfo.id && projects.TryAdd(project.id,project))
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
				scannedUserNames.TryAdd(follower.username,0);
				scanTasks.Add(ScanProjects(accessor.GetFavoriteProjects(follower.username), scan_depth));
            }

			await Task.WhenAll(scanTasks);

			scanTasks.Clear();

			List<Task> studioTasks = new List<Task>();

			var projectsToScan = new List<ProjectInfo>(listedProjects);

            for (int i = 0; i < projectsToScan.Count; i++)
            {
				int depth = 0;

				await foreach (var studio in accessor.GetProjectStudios(username, projectsToScan[i].id))
				{
					async Task ScanManagers()
                    {
                        await foreach (var manager in accessor.GetStudioManagers(studio.id))
                        {
                            if (scannedUserNames.TryAdd(manager.username,0))
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
				async Task ScanRemixes(ProjectInfo project, uint innerScanDepth)
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

			async Task Download(ProjectInfo project)
			{
				project.author.username = username;
				var dir = await accessor.DownloadAndExportProject(project, directory);
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

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "projects.json", JsonConvert.SerializeObject(listedProjects.ToArray(),Formatting.Indented));

			await Task.WhenAll(downloadTasks);
			Console.WriteLine("Done!");
			Console.WriteLine("Total Projects Found = " + foundProjects);
			Console.WriteLine("Total Projects Downloaded = " + completedDownloads);
            if (foundProjects != completedDownloads)
            {
				Console.WriteLine($"{foundProjects - completedDownloads} Projects have been found to be deleted");
			}
		}

		static async Task DownloadAllStudiosCurrentProfile(ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
			directory = directory.CreateSubdirectory("Studios");
			List<Task> downloadTasks = new List<Task>();
			List<UserStudioInfo> studios = new List<UserStudioInfo>();
			await foreach (var studio in accessor.GetStudios())
			{
				var studioDirectory = directory.CreateSubdirectory(ScratchAPI.FilterName(studio.fields.title));
				var projectsDirectory = studioDirectory.CreateSubdirectory("Projects");

				studios.Add(studio);

				await foreach (var project in accessor.GetProjectsInStudio(studio))
                {
					async Task Download()
					{
						var info = await accessor.GetProjectInfo(project.id);
                        if (info == null)
                        {
							info = (ProjectInfo)project;
                        }
						var dir = await accessor.DownloadAndExportProject(info, directory);
						if (dir != null && downloadComments)
						{
							await DownloadProjectComments(accessor, project.username, project.id, dir);
						}
					}

					downloadTasks.Add(Download());
					//downloadTasks.Add(accessor.DownloadAndExportProject(project.id, projectsDirectory));
				}
			}

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "studios.json", JsonConvert.SerializeObject(studios, Formatting.Indented));
			await Task.WhenAll(downloadTasks);

			Console.WriteLine("Done downloading studios!");
		}

		static async Task DownloadFavoriteProjects(string username, ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
			directory = directory.CreateSubdirectory("Favorites");
			List<Task> downloadTasks = new List<Task>();

			List<ProjectInfo> projects = new List<ProjectInfo>();

			int counter = 0;
			int downloadedCounter = 0;
            await foreach (var favorite in accessor.GetFavoriteProjects(username))
            {
				Console.WriteLine($"Found Favorite = {favorite.title}-{favorite.id}");
				counter++;

				async Task Download()
				{
					var dir = await accessor.DownloadAndExportProject(favorite, directory);
					if (dir != null && downloadComments)
					{
						Interlocked.Increment(ref downloadedCounter);
						await DownloadProjectComments(accessor, favorite.author.username, favorite.id, dir);
					}
				}

				downloadTasks.Add(Download());

				//downloadTasks.Add(accessor.DownloadAndExportProject(favorite, directory));
				projects.Add(favorite);
			}

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "favorites.json", JsonConvert.SerializeObject(projects, Formatting.Indented));

			await Task.WhenAll(downloadTasks);

			Console.WriteLine($"Found Favorites : {counter}");
			Console.WriteLine($"Downloaded Favorites : {downloadedCounter}");
		}

		static async Task DownloadFollowers(string username, ScratchAPI accessor, DirectoryInfo directory)
        {
			directory = directory.CreateSubdirectory("Followers");
			List<Task> downloadTasks = new List<Task>();
			List<UserInfo> followers = new List<UserInfo>();
			await foreach (var follower in accessor.GetFollowers(username))
            {
				followers.Add(follower);

				var subDir = directory.CreateSubdirectory(ScratchAPI.FilterName(follower.username));

				async Task Downloader()
                {
					Console.WriteLine($"Downloading: {follower.username}");
					var image = follower.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

					var imgStream = await accessor.DownloadFromURL(image.Value);

					using var pngFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "thumbnail.gif");
					using var gifFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "thumbnail.png");

					await imgStream.CopyToAsync(pngFile);

					imgStream.Position = 0;

					await imgStream.CopyToAsync(gifFile);

					using var jsonFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "info.json");

					using var writer = new StreamWriter(jsonFile);

					await writer.WriteAsync(JsonConvert.SerializeObject(follower,Formatting.Indented));

					Console.WriteLine($"Done: {follower.username}");
				}

				downloadTasks.Add(Downloader());
				//await Downloader();

			}
			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "followers.json", JsonConvert.SerializeObject(followers, Formatting.Indented));

			await Task.WhenAll(downloadTasks);
		}

		static async Task DownloadFollowing(string username, ScratchAPI accessor, DirectoryInfo directory)
		{
			List<UserInfo> following = new List<UserInfo>();
			directory = directory.CreateSubdirectory("Following");
			List<Task> downloadTasks = new List<Task>();
			await foreach (var followingUser in accessor.GetFollowingUsers(username))
			{
				following.Add(followingUser);

				var subDir = directory.CreateSubdirectory(ScratchAPI.FilterName(followingUser.username));

				async Task Downloader()
				{
					Console.WriteLine($"Downloading: {followingUser.username}");
					var image = followingUser.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

					var imgStream = await accessor.DownloadFromURL(image.Value);

					using var pngFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "thumbnail.gif");
					using var gifFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "thumbnail.png");

					await imgStream.CopyToAsync(pngFile);

					imgStream.Position = 0;

					await imgStream.CopyToAsync(gifFile);

					using var jsonFile = File.Create(Utilities.PathAddBackslash(subDir.FullName) + "info.json");

					using var writer = new StreamWriter(jsonFile);

					await writer.WriteAsync(JsonConvert.SerializeObject(followingUser, Formatting.Indented));

					Console.WriteLine($"Done: {followingUser.username}");
				}

				downloadTasks.Add(Downloader());

			}

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "following.json", JsonConvert.SerializeObject(following, Formatting.Indented));

			await Task.WhenAll(downloadTasks);
		}

		static async Task DownloadProfileInfo(string username, ScratchAPI accessor, DirectoryInfo directory)
        {
			var userInfo = await accessor.GetUserInfo(username);

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "profile.json", JsonConvert.SerializeObject(userInfo,Formatting.Indented));
        }

		class DownloadedComment
        {
			public CommentInfo Comment;
			public List<CommentInfo> Replies;
        }

		static async Task DownloadProjectComments(ScratchAPI accessor, string username, long project_id, DirectoryInfo directory)
        {

			List<DownloadedComment> downloadedComments = new List<DownloadedComment>();

			List<Task<DownloadedComment>> commentDownloads = new List<Task<DownloadedComment>>();

			await foreach (var comment in accessor.GetProjectComments(username,project_id))
            {
				async Task<DownloadedComment> Download(CommentInfo comment)
                {
					List<CommentInfo> replies = new List<CommentInfo>();
                    if (comment.reply_count > 0)
                    {
						await foreach (var reply in accessor.GetRepliesToComment(username, project_id, comment))
						{
							replies.Add(reply);
						}
					}

					return new DownloadedComment
					{
						Comment = comment,
						Replies = replies
					};
				}

				commentDownloads.Add(Download(comment));
			}

			await Task.WhenAll(commentDownloads);

			downloadedComments.AddRange(commentDownloads.Select(t => t.Result));
            if (downloadedComments.Count > 0)
            {
				await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "comments.json", JsonConvert.SerializeObject(downloadedComments, Formatting.Indented));
			}
        }

		static DirectoryInfo GetDirectory()
        {
            while (true)
            {
				Console.Write("Specify output directory: ");
				var directory = Console.ReadLine();
				try
				{
					return Directory.CreateDirectory(directory);
				}
				catch (Exception)
				{
					Console.WriteLine("Invalid Directory. Try Again");
				}
			}
		}

		static string GetStringFromConsole(string message)
        {
			while (true)
			{
				Console.WriteLine($"{message}: ");
				var result = Console.ReadLine();
                if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
                {
					continue;
                }
                else
                {
					return result;
                }
			}
		}

		static uint GetUIntFromConsole(string message, uint? defaultValue)
        {
			while (true)
			{
                if (defaultValue == null)
                {
					Console.WriteLine($"{message}: ");
				}
				else
                {
					Console.WriteLine($"{message} (Default Value - {defaultValue.Value}): ");
				}
				var result = Console.ReadLine();
				if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
				{
                    if (defaultValue != null)
                    {
						return defaultValue.Value;
                    }
				}
				else
				{
                    if (uint.TryParse(result,out var number))
                    {
						return number;
                    }
					else
                    {
						Console.WriteLine("Invalid Number. Try Again");
                    }
				}
			}
		}

		static bool PickOption<EnumType>(string message, out EnumType option, IEnumerable<string> descriptions = null) where EnumType : Enum
		{
			return PickOption<EnumType>(message, out option, (EnumType)(object)-1,descriptions);
		}

		static bool PickOption<EnumType>(string message, out EnumType option, EnumType defaultValue, IEnumerable<string> descriptions = null) where EnumType : Enum
        {
			bool useDefault = ((int)(object)defaultValue) != -1;

			if (descriptions == null)
			{
				descriptions = Enumerable.Empty<string>();
			}
			Console.WriteLine(message);
			Console.WriteLine("--------------------------------");
			var options = (EnumType[])Enum.GetValues(typeof(EnumType));

			var descArray = descriptions.ToArray();

			int index = 0;

			foreach (var val in options)
			{
				string defaultText;
				if (useDefault && val.Equals(defaultValue))
				{
					defaultText = "(Default)";
				}
				else
				{
					defaultText = "";
				}


				if (index < descArray.Length)
				{
					Console.WriteLine($"{(int)(object)val} - {Prettify(val.ToString())} {defaultText} : {descArray[index]}");
				}
				else
				{
					Console.WriteLine($"{(int)(object)val} - {Prettify(val.ToString())} {defaultText}");
				}


				index++;
			}

			Console.WriteLine("--------------------------------");
			while (true)
			{
				Console.WriteLine("Enter number to select:");
				var inputLine = Console.ReadLine();
				if (int.TryParse(inputLine, out var input) && Enum.IsDefined(typeof(EnumType), (EnumType)(object)input))
				{
					option = (EnumType)Enum.ToObject(typeof(EnumType), input);
					if (options.Contains(option))
					{
						return true;
					}
					else
					{
						Console.WriteLine("Invalid Option - Try Again");
					}
				}
                else if ((string.IsNullOrEmpty(inputLine) || string.IsNullOrWhiteSpace(inputLine)) && useDefault)
                {
					option = defaultValue;
					return true;
                }
				else
				{
					Console.WriteLine("Invalid Number - Try Again");
				}
			}
		}
	}
}
