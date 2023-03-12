using ScratchDL.CMD.Options.Base;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    public sealed class DownloadProfileInfo : ProgramOption_Base
    {
        public override string Title => "Download Profile Info";

        public override string Description => "Downloads profile information about a username";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] users = Utilities.GetStringFromConsole("Enter username (or multiple seperated by commas)").Split(',', ' ');
            foreach (string user in users)
            {
                await DownloadProfileInfoOfUser(user, accessor, directory);
            }

            return false;
        }

        public static async Task DownloadProfileInfoOfUser(string username, ScratchAPI accessor, DirectoryInfo directory)
        {
            User? userInfo = await accessor.GetUserInfo(username);

            await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "profile.json", JsonSerializer.Serialize(userInfo, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
