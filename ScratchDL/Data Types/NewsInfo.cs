namespace ScratchDL
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
