using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader.Options
{
    public abstract class ProgramOption_Base
    {
		class DownloadedComment
		{
			public Comment Comment;
			public List<Comment> Replies;
		}

		public abstract string Title { get; }
        public abstract string Description { get;  }

        public abstract Task<bool> Run(ScratchAPI accessor);

		public static async Task DownloadProjectComments(ScratchAPI accessor, string username, long project_id, DirectoryInfo directory)
		{
			List<DownloadedComment> downloadedComments = new List<DownloadedComment>();

			List<Task<DownloadedComment>> commentDownloads = new List<Task<DownloadedComment>>();

			await foreach (var comment in accessor.GetProjectComments(username, project_id))
			{
				async Task<DownloadedComment> Download(Comment comment)
				{
					List<Comment> replies = new List<Comment>();
					if (comment.reply_count > 0)
					{
						await foreach (var reply in accessor.GetRepliesToComment(username, project_id, comment))
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

			await Task.WhenAll(commentDownloads);

			downloadedComments.AddRange(commentDownloads.Select(t => t.Result));
			if (downloadedComments.Count > 0)
			{
				await File.WriteAllTextAsync(Utilities.PathAddBackslash(directory.FullName) + "comments.json", JsonConvert.SerializeObject(downloadedComments, Formatting.Indented));
			}
		}
	}
}
