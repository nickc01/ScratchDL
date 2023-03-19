using System.Text.Json.Serialization;

namespace ScratchDL
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
	) : IStudio
    {
		long IStudio.ID => id;

		string IStudio.Title => title;

		DateTime IStudio.DateCreated => history.created;

		DateTime IStudio.DateModified => history.modified;

		string IStudio.ThumbnailURL => image;

        public record class History(
			DateTime created,
			DateTime modified
		);
	}
}
