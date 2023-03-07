using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadFollowers : ProgramOption_Base
    {
		public override string Title => "Download Followers";

		public override string Description => "Downloads all users following a certain user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
			var directory = Utilities.GetDirectory();
			var users = Utilities.GetStringFromConsole("Enter username to get followers from (or multiple seperated by commas)").Split(',', ' ');

			foreach (var user in users)
			{
				var userDirectory = directory.CreateSubdirectory(user);
				await DownloadUserFollowers(user, accessor, userDirectory);
			}
			return false;
		}

		public static async Task DownloadUserFollowers(string username, ScratchAPI accessor, DirectoryInfo directory)
		{
			directory = directory.CreateSubdirectory("Followers");
			List<Task> downloadTasks = new List<Task>();
			List<User> followers = new List<User>();
			await foreach (var follower in accessor.GetFollowers(username))
			{
				followers.Add(follower);

				var subDir = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(follower.username));

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

					await writer.WriteAsync(JsonConvert.SerializeObject(follower, Formatting.Indented));

					Console.WriteLine($"Done: {follower.username}");
				}

				downloadTasks.Add(Downloader());
			}
			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "followers.json", JsonConvert.SerializeObject(followers, Formatting.Indented));

			await Task.WhenAll(downloadTasks);
		}
	}
}
