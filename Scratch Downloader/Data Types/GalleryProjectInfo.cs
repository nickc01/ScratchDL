using System;

namespace Scratch_Downloader
{
    public class GalleryProjectInfo
	{
		public class Creator
		{
			public string username;
			public long pk;
			public string thumbnail_url;
			public bool admin;
		}

		public class Fields
		{
			public long view_count;
			public long favorite_count;
			public long remixers_count;
			public Creator creator;
			public string title;
			public bool isPublished;
			public DateTime datetime_created;
			public string thumbnail_url;
			public string visibility;
			public long love_count;
			public DateTime datetime_modified;
			public string uncached_thumbnail_url;
			public string thumbnail;
			public DateTime? datetime_shared;
			public long commenters_count;
		}

		public Fields fields;
		public string model;

		/// <summary>
		/// The id of the project
		/// </summary>
		public long pk;
	}
}
