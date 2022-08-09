using System;

namespace Scratch_Downloader
{
	public class StudioInfo
	{
		public class History
		{
			public DateTime created;
			public DateTime modified;
		}

		public long id;
		public string title;
		public long host;
		public string description;
		public string visibility;
		[Newtonsoft.Json.JsonProperty("public")]
		public bool is_public;
		public bool open_to_all;
		public bool comments_allowed;
		public string image;
		public History history;

	}
}
