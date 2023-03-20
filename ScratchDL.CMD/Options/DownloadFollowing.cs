using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    internal sealed class DownloadFollowing : ProgramOption_Base
    {
        public override string Title => "Download Following";

        public override string Description => "Downloads all users a certain user is following";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] users = Utilities.GetStringFromConsole("Enter username to get followers from (or multiple seperated by commas)").Split(',', ' ');

            foreach (string user in users)
            {
                DirectoryInfo userDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(user));
                await DownloadFollowingOfUser(user, accessor, userDirectory);
            }
            return false;
        }

        public static async Task DownloadFollowingOfUser(string username, ScratchAPI accessor, DirectoryInfo directory)
        {
            List<User> following = new();
            directory = directory.CreateSubdirectory("Following");
            List<Task> downloadTasks = new();
            await foreach (User followingUser in accessor.GetFollowingUsers(username))
            {
                following.Add(followingUser);

                DirectoryInfo subDir = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(followingUser.username));

                async Task Downloader()
                {
                    Console.WriteLine($"Downloading: {followingUser.username}");
                    KeyValuePair<string, string> image = followingUser.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

                    {
                        using Stream imgStream = await accessor.DownloadProfileImage(followingUser.id);

                        using FileStream pngFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "thumbnail.gif", FileMode.Create, FileAccess.Write);
                        using FileStream gifFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "thumbnail.png", FileMode.Create, FileAccess.Write);

                        await imgStream.CopyToAsync(pngFile);

                        imgStream.Position = 0;

                        await imgStream.CopyToAsync(gifFile);
                    }

                    using FileStream jsonFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(subDir.FullName) + "info.json", FileMode.Create, FileAccess.Write);

                    using StreamWriter writer = new(jsonFile);

                    await writer.WriteAsync(JsonSerializer.Serialize(followingUser, new JsonSerializerOptions() { WriteIndented = true }));

                    Console.WriteLine($"Done: {followingUser.username}");
                }

                downloadTasks.Add(Downloader());

            }

            await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "following.json", JsonSerializer.Serialize(following.OrderBy(f => f.username), new JsonSerializerOptions() { WriteIndented = true }));

            await Task.WhenAll(downloadTasks);

            Console.WriteLine($"Following Users Downloaded : {following.Count}");
        }
    }
}
