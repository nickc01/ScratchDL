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
    public class DownloadAllFollowersFromUser : DownloadMode
    {
        public DownloadAllFollowersFromUser(MainWindowViewModel viewModel) : base(viewModel) { }

        public override string Name => "Download All Followers From User";

        public override string Description => "Download all followers of a certain user";

        public string Username = string.Empty;

        List<User> downloadedFollowers = new List<User>();

        public override async Task Download(DownloadData data)
        {
            downloadedFollowers.Clear();
            await foreach (var follower in data.API.GetFollowers(Username))
            {
                downloadedFollowers.Add(follower);
                data.AddEntry(new DownloadEntry(true,follower.id,follower.username,string.Empty));
            }
        }

        public override async Task Export(ExportData data)
        {
            foreach (var follower in downloadedFollowers)
            {
                Debug.WriteLine($"Downloading: {follower.username}");
                KeyValuePair<string, string> image = follower.profile.images.MaxBy(kv => int.Parse(kv.Key.Split('x')[0]));

                {
                    using Stream imgStream = await data.API.DownloadProfileImage(follower.id);

                    using FileStream pngFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "thumbnail.gif", FileMode.Create, FileAccess.Write);
                    using FileStream gifFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "thumbnail.png", FileMode.Create, FileAccess.Write);

                    await imgStream.CopyToAsync(pngFile);

                    imgStream.Position = 0;

                    await imgStream.CopyToAsync(gifFile);
                }

                using FileStream jsonFile = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(data.FolderPath.FullName) + "info.json", FileMode.Create, FileAccess.Write);

                using StreamWriter jsonWriter = new StreamWriter(jsonFile);

                await jsonWriter.WriteAsync(JsonSerializer.Serialize(follower, new JsonSerializerOptions() { WriteIndented = true }));

                Debug.WriteLine($"Done: {follower.username}");
                data.WriteToConsole($"✔️ Finished : {follower.username}");
            }
        }
    }

    public class DownloadAllFollowersFromUserUI : DownloadModeUI<DownloadAllFollowersFromUser>
    {
        public DownloadAllFollowersFromUserUI(DownloadAllFollowersFromUser modeObject) : base(modeObject) { }

        public override string Column3 => string.Empty;

        public override void Setup(StackPanel controlsPanel)
        {
            CreateAndAddTextBox(nameof(ModeObject.Username), controlsPanel);
        }
    }
}
