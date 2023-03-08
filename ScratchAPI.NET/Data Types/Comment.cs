namespace Scratch_Downloader
{
    public record class Comment(
        long id,
        long? parent_id,
        long? commentee_id,

        string content,

        DateTime datetime_created,
        DateTime datetime_modified,
        string visiblity,

        Comment.Author author,

        long reply_count
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
