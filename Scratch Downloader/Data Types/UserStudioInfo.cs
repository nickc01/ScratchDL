namespace Scratch_Downloader
{
    public class UserStudioInfo
    {
        public class Fields
        {
			public int curators_count;
			public int projecters_count;
			public string title;
			public string datetime_created;
			public string thumbnail_url;
			public int commenters_count;
			public string datetime_modified;
			public GalleryProjectInfo owner;
        }


		public string model;
		public long pk;
		public Fields fields;
    }
}
