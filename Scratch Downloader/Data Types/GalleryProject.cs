using Newtonsoft.Json;
using System;

namespace Scratch_Downloader
{
    public record GalleryProject (
		GalleryProject.Fields fields,
		string model,
		[field:JsonProperty("pk")] long id
		)
	{
		public record class Creator
		(
			string username,
			[field:JsonProperty("pk")] long id,
			string thumbnail_url,
			bool admin
		);

		public record class Fields (
			long view_count,
			long favorite_count,
			long remixers_count,
			Creator creator,
			string title,
			bool isPublished,
			DateTime datetime_created,
			string thumbnail_url,
			string visibility,
			long love_count,
			DateTime datetime_modified,
			string uncached_thumbnail_url,
			string thumbnail,
			DateTime? datetime_shared,
			long commenters_count
		);
	}
}
