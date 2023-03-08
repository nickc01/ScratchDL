namespace Scratch_Downloader
{
    public record class ProjectComment(
        long id,
        long? parent_id,
        long? commentee_id,
        string content,
        DateTime datetime_created,
        DateTime datetime_modified,
        ProjectComment.Author author,
        long reply_count,
        string visibility
    )
    {
        public record class Author(
            long id,
            string username,
            bool scratchteam,
            string image
        );
    }
}
