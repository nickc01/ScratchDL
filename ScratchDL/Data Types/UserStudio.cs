using System.Text.Json.Serialization;

namespace ScratchDL
{
    public record class UserStudio(
        string model,
        [property: JsonPropertyName("pk")]
        long id,
        UserStudio.Fields fields
    ) : IStudio
    {
        long IStudio.ID => id;

        string IStudio.Title => fields.title;

        DateTime IStudio.DateCreated => fields.datetime_created;

        DateTime IStudio.DateModified => fields.datetime_modified;

        string IStudio.ThumbnailURL => fields.thumbnail_url;

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
    }
}
