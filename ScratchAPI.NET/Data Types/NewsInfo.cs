namespace Scratch_Downloader
{
    public record class News(
        long id,
        string stamp,
        string headline,
        string url,
        string image,
        string copy
    );
}
