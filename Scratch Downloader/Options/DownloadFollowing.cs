using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadFollowing : ProgramOption_Base
    {
		public override string Title => "Download Following";

		public override string Description => "Downloads all users a certain user is following";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
			var directory = Utilities.GetDirectory();
			var users = Utilities.GetStringFromConsole("Enter username to get followers from (or multiple seperated by commas)").Split(',', ' ');

			foreach (var user in users)
			{
				var userDirectory = directory.CreateSubdirectory(user);
				await DownloadFollowingOfUser(user, accessor, userDirectory);
			}
			return false;
		}

		public static async Task DownloadFollowingOfUser(string username, ScratchAPI accessor, DirectoryInfo directory)
		{
			List<User> following = new List<User>();
			directory = directory.CreateSubdirectory("Following");
			List<Task> downloadTasks = new List<Task>();
			await foreach (var followingUser in accessor.GetFollowingUsers(username))
			{
				following.Add(followingUser);

				var subDir = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(followingUser.username));

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
	}
}
