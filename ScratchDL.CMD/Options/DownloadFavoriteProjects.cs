using ScratchDL.CMD.Options.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    internal sealed class DownloadFavoriteProjects : ProgramOption_Base
    {
        public override string Title => "Download Favorite Projects";
        public override string Description => "Downloads all favorite projects from a user";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            DirectoryInfo directory = Utilities.GetDirectory();
            string[] users = Utilities.GetStringFromConsole("Enter username to get projects from (or multiple seperated by commas)").Split(',', ' ');
            bool downloadComments = Utilities.GetCommentDownloadOption();

            foreach (string user in users)
            {
                DirectoryInfo userDirectory = directory.CreateSubdirectory(Utilities.RemoveIllegalCharacters(user));
                await DownloadProjects(user, accessor, userDirectory, downloadComments);
            }
            return false;
        }

        public static async Task DownloadProjects(string username, ScratchAPI accessor, DirectoryInfo directory, bool downloadComments)
        {
            directory = directory.CreateSubdirectory("Favorites");
            List<Task> downloadTasks = new();

            List<Project> projects = new();

            int counter = 0;
            int downloadedCounter = 0;
            await foreach (Project favorite in accessor.GetFavoriteProjects(username))
            {
                Console.WriteLine($"Found Favorite = {favorite.title}-{favorite.id}");
                counter++;

                async Task Download()
                {
                    try
                    {
                        DirectoryInfo dir = await accessor.DownloadAndExportProject(favorite, directory);
                        if (downloadComments)
                        {
                            _ = Interlocked.Increment(ref downloadedCounter);
                            await DownloadProjectComments(accessor, favorite.author.username, favorite.id, dir);
                        }
                    }
                    catch (ProjectDownloadException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                downloadTasks.Add(Download());

                projects.Add(favorite);
            }

            await WriteTextToFile(Helpers.PathAddBackslash(directory.FullName) + "favorites.json", JsonSerializer.Serialize(projects.OrderBy(p => p.title).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));

            await Task.WhenAll(downloadTasks);

            Console.WriteLine($"Found Favorites : {counter}");
            Console.WriteLine($"Downloaded Favorites : {downloadedCounter}");
        }
    }
}
