using System;

namespace Scratch_Downloader
{
	public class ProjectComment
	{
		public class Author
		{
			public long id;
			public string username;
			public bool scratchteam;
			public string image;
		}


		public long id;
		public long? parent_id;
		public long? commentee_id;
		public string content;
		public DateTime datetime_created;
		public DateTime datetime_modified;
		public Author author;
		public long reply_count;
		public string visibility;
	}
}
