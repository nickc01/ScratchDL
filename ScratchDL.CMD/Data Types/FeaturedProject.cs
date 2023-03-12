namespace Scratch_Downloader
{
	public record class FeaturedProject (
		string creator,
		long id,
		long love_count,
		string thumbnail_url,
		string title,
		string type,
		long? remixers_count
	);
}
