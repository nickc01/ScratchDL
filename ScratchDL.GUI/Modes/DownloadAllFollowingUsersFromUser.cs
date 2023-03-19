using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Modes
{
    public class DownloadAllFollowingUsersFromUser : DownloadMode
    {
        public DownloadAllFollowingUsersFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Users a User is Following";

        public override string Description => "Download all the people a user is following";

        public string Username = string.Empty;

        List<User> downloadedFollowing = new List<User>();

        public override async Task Download(DownloadData data)
        {
            downloadedFollowing.Clear();
            await foreach (var follower in data.API.GetFollowingUsers(Username))
            {
                downloadedFollowing.Add(follower);
                data.AddEntry(new DownloadEntry(true, follower.id, follower.username, string.Empty));
            }
        }

        public override async Task Export(ExportData data)
        {
            foreach (var followingUser in downloadedFollowing)
            {
                Debug.WriteLine($"Downloading: {followingUser.username}");
                KeyValuePair<string, string> image = followingUser.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

                {
                    using Stream imgStream = await data.API.DownloadProfileImage(followingUser.id);

                    using FileStream pngFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "thumbnail.gif", FileMode.Create, FileAccess.Write);
                    using FileStream gifFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "thumbnail.png", FileMode.Create, FileAccess.Write);

                    await imgStream.CopyToAsync(pngFile);

                    imgStream.Position = 0;

                    await imgStream.CopyToAsync(gifFile);
                }

                using FileStream jsonFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "info.json", FileMode.Create, FileAccess.Write);

                using StreamWriter jsonWriter = new StreamWriter(jsonFile);

                await jsonWriter.WriteAsync(JsonSerializer.Serialize(followingUser, new JsonSerializerOptions() { WriteIndented = true }));

                Debug.WriteLine($"Done: {followingUser.username}");
                data.WriteToConsole($"✔️ Finished : {followingUser.username}");
            }
        }
    }

    public class DownloadAllFollowingUsersFromUserUI : DownloadModeUI<DownloadAllFollowingUsersFromUser>
    {
        public DownloadAllFollowingUsersFromUserUI(DownloadAllFollowingUsersFromUser modeObject) : base(modeObject) { }

        public override string Column3 => string.Empty;

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
        }
    }
}
