using Newtonsoft.Json;
using Scratch_Downloader.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadFavoriteProjects : ProgramOption_Base
    {
        public override string Title => "Download Favorite Projects";
        public override string Description => "Downloads all favorite projects from a user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            var directory = Utilities.GetDirectory();
            var users = Utilities.GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');
            var downloadComments = Utilities.GetCommentDownloadOption();

            foreach (var user in users)
            {
                var userDirectory = directory.CreateSubdirectory(user);
                await DownloadProjects(user, accessor, userDirectory, downloadComments);
            }
            return false;
        }

		public static async Task DownloadProjects(string username, ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
		{
			directory = directory.CreateSubdirectory("Favorites");
			List<Task> downloadTasks = new List<Task>();

			List<Project> projects = new List<Project>();

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

				projects.Add(favorite);
			}

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "favorites.json", JsonConvert.SerializeObject(projects, Formatting.Indented));

			await Task.WhenAll(downloadTasks);

			Console.WriteLine($"Found Favorites : {counter}");
			Console.WriteLine($"Downloaded Favorites : {downloadedCounter}");
		}
	}
}
