using Newtonsoft.Json;
using System;

namespace Scratch_Downloader
{
    public record class UserStudio (
		string model,
		[field:JsonProperty("pk")]
		long id,
		UserStudio.Fields fields
	)
    {
        public record class Fields
        (
			int curators_count,
			int projecters_count,
			string title,
			DateTime datetime_created,
			string thumbnail_url,
			int commenters_count,
			DateTime datetime_modified,
			GalleryProject.Creator owner
        );

		/*public static explicit operator Studio(StudioUser info)
		{
			return new StudioInfo
			{
				history = new Studio.History
				{
					created = info.fields.datetime_created,
					modified = info.fields.datetime_modified
				},
				id = info.id,
				host = info.fields.owner.id,
				image = info.fields.thumbnail_url,
				title = info.fields.title
			};
		}*/
	}
}
