namespace ScratchDL
{
    public record class StudioProject(
        long id,
        string title,
        string image,
        long creator_id,
        string username,
        Dictionary<string, string> avatar,
        long actor_id
    ) : IProject
    {
        string IProject.Title => title;

        long IProject.ID => id;

        DateTime IProject.DateCreated => DateTime.UnixEpoch;

        DateTime IProject.DateModified => DateTime.UnixEpoch;

        DateTime? IProject.DateShared => DateTime.UnixEpoch;

        string IProject.AuthorUsername => username;

        long IProject.AuthorID => creator_id;

        long IProject.Views => 0;

        long IProject.Loves => 0;

        long IProject.Favorites => 0;

        long IProject.Remixes => 0;

        string IProject.ThumbnailImage => string.Empty;

        string IProject.Visibility => string.Empty;

        bool IProject.IsPublished => true;
    }
}
