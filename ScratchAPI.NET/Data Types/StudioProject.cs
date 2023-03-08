namespace Scratch_Downloader
{
    public record class StudioProject(
        long id,
        string title,
        string image,
        long creator_id,
        string username,
        Dictionary<string, string> avatar,
        long actor_id
    );
}
