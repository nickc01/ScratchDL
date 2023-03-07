using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public sealed class DownloadProfileInfo : ProgramOption_Base
    {
		public override string Title => "Download Profile Info";

		public override string Description => "Downloads profile information about a username";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
			var directory = Utilities.GetDirectory();
			var users = Utilities.GetStringFromConsole("Enter username (or multiple seperated by commas)").Split(',', ' ');
            foreach (var user in users)
            {
				await DownloadProfileInfoOfUser(user,accessor,directory);
			}

			return false;
		}

		public static async Task DownloadProfileInfoOfUser(string username, ScratchAPI accessor, DirectoryInfo directory)
		{
			var userInfo = await accessor.GetUserInfo(username);

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "profile.json", JsonConvert.SerializeObject(userInfo, Formatting.Indented));
		}
	}
}
