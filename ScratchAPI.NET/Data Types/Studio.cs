//using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Scratch_Downloader
{
	public record class Studio(
		long id,
		string title,
		long host,
		string description,
		string visibility,
		[property:JsonPropertyName("public")]
		bool is_public,
		bool open_to_all,
		bool comments_allowed,
		string image,
		Studio.History history
	)
	{
		public record class History(
			DateTime created,
			DateTime modified
		);

		/*public Studio(UserStudio info) : this
		(
			info.id,
			info.fields.title,
			info.fields.owner.id,
			"",
			"visible",
			true,
			true,
			true,
			"",
			new History(info.fields.datetime_created,info.fields.datetime_modified)
		) { }*/
	}
}
