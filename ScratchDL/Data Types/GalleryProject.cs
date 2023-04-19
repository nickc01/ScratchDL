using System.Text.Json.Serialization;

namespace ScratchDL
{
    public record GalleryProject(
        GalleryProject.Fields fields,
        string model,
        [property: JsonPropertyName("pk")] long id
        ) : IProject
    {
        string IProject.Title => fields.title;

        long IProject.ID => id;

        DateTime IProject.DateCreated => fields.datetime_created;

        DateTime IProject.DateModified => fields.datetime_modified;

        DateTime? IProject.DateShared => fields.datetime_shared;

        string IProject.AuthorUsername => fields.creator.username;

        long IProject.Views => fields.view_count;

        long IProject.Loves => fields.love_count;

        long IProject.Favorites => fields.favorite_count;

        long IProject.Remixes => fields.remixers_count;

        string IProject.ThumbnailImage => fields.thumbnail_url;

        string IProject.Visibility => fields.visibility;

        bool IProject.IsPublished => fields.isPublished;

        long IProject.AuthorID => fields.creator.id;

        public record class Creator
        (
            string username,
            [property: JsonPropertyName("pk")] long id,
            string thumbnail_url,
            bool admin
        );

        public record class Fields(
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
