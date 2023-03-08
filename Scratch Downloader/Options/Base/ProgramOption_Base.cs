﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options.Base
{
    public abstract class ProgramOption_Base
    {
        private class DownloadedComment
        {
            public Comment? Comment;
            public List<Comment>? Replies;
        }

        public abstract string Title { get; }
        public abstract string Description { get; }

        public abstract Task<bool> Run(ScratchAPI accessor);

        public static async Task DownloadProjectComments(ScratchAPI accessor, string username, long project_id, DirectoryInfo directory)
        {
            List<DownloadedComment> downloadedComments = new();

            List<Task<DownloadedComment>> commentDownloads = new();

            await foreach (Comment comment in accessor.GetProjectComments(username, project_id))
            {
                async Task<DownloadedComment> Download(Comment comment)
                {
                    List<Comment> replies = new();
                    if (comment.reply_count > 0)
                    {
                        await foreach (Comment reply in accessor.GetRepliesToComment(username, project_id, comment))
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

                for (int i = 0; i < 100; i++)
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
                }
            }
        }
    }
}
