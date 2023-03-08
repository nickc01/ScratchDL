using System.Text.Json.Serialization;

namespace Scratch_Downloader
{
    public record class UserStudio(
        string model,
        [property: JsonPropertyName("pk")]
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
    }
}
