using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public abstract class DownloadMode
    {
        private class DownloadedComment
        {
            public Comment? Comment;
            public List<Comment>? Replies;
        }

        public readonly MainWindowViewModel ViewModel;

        public DownloadMode(MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract Task Download(ScratchAPI api, Action<ProjectEntry> addEntry, Action<double> setProgress);

        public abstract Task Export(ScratchAPI api, DirectoryInfo folderPath, IEnumerable<long> selectedIDs, Action<string> writeToConsole, Action<double> setProgress);

        protected static async Task DownloadProjectComments(ScratchAPI api, string username, long project_id, DirectoryInfo directory)
        {
            List<DownloadedComment> downloadedComments = new();

            List<Task<DownloadedComment>> commentDownloads = new();

            await foreach (Comment comment in api.GetProjectComments(username, project_id))
            {
                async Task<DownloadedComment> Download(Comment comment)
                {
                    List<Comment> replies = new();
                    if (comment.reply_count > 0)
                    {
                        await foreach (Comment reply in api.GetRepliesToComment(username, project_id, comment))
                        {
                            replies.Add(reply);
                        }
                    }

                    return new DownloadedComment
                    {
                        Comment = comment,
                        Replies = replies
                    };
                }

                commentDownloads.Add(Download(comment));
            }

            _ = await Task.WhenAll(commentDownloads);

            downloadedComments.AddRange(commentDownloads.Select(t => t.Result));
            if (downloadedComments.Count > 0)
            {
                using (var fileStream = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(directory.FullName) + "comments.json", FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(fileStream))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(downloadedComments, new JsonSerializerOptions() { WriteIndented = true }));
                    }
                }



                /*for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "comments.json", JsonSerializer.Serialize(downloadedComments, new JsonSerializerOptions() { WriteIndented = true }));
                        break;
                    }
                    catch (Exception)
                    {
                        if (i == 99)
                        {
                            throw;
                        }
                        continue;
                    }
                }*/
            }
        }
    }
}
