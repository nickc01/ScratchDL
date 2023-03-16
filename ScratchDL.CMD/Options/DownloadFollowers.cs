using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    public sealed class DownloadFollowers : ProgramOption_Base
    {
        public override string Title => "Download Followers";

        public override string Description => "Downloads all users following a certain user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] users = Utilities.GetStringFromConsole("Enter username to get followers from (or multiple seperated by commas)").Split(',', ' ');

            foreach (string user in users)
            {
                DirectoryInfo userDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(user));
                await DownloadUserFollowers(user, accessor, userDirectory);
            }
            return false;
        }

        public static async Task DownloadUserFollowers(string username, ScratchAPI accessor, DirectoryInfo directory)
        {
            directory = directory.CreateSubdirectory("Followers");
            List<Task> downloadTasks = new();
            List<User> followers = new();
            await foreach (User follower in accessor.GetFollowers(username))
            {
                followers.Add(follower);

                DirectoryInfo subDir = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(follower.username));

                async Task Downloader()
                {
                    Console.WriteLine($"Downloading: {follower.username}");
                    KeyValuePair<string, string> image = follower.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

                    {
                        using Stream imgStream = await accessor.DownloadProfileImage(follower.id);

                        using FileStream pngFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "thumbnail.gif", FileMode.Create, FileAccess.Write);
                        using FileStream gifFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "thumbnail.png", FileMode.Create, FileAccess.Write);

                        await imgStream.CopyToAsync(pngFile);

                        imgStream.Position = 0;

                        await imgStream.CopyToAsync(gifFile);
                    }

                    using FileStream jsonFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "info.json", FileMode.Create, FileAccess.Write);

                    using StreamWriter jsonWriter = new StreamWriter(jsonFile);

                    await jsonWriter.WriteAsync(JsonSerializer.Serialize(follower, new JsonSerializerOptions() { WriteIndented = true }));

                    Console.WriteLine($"Done: {follower.username}");
                }

                downloadTasks.Add(Downloader());
            }
            await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "followers.json", JsonSerializer.Serialize(followers.OrderBy(f => f.username), new JsonSerializerOptions() { WriteIndented = true }));

            await Task.WhenAll(downloadTasks);

            Console.WriteLine($"Followers Downloaded : {followers.Count}");
        }
    }
}
