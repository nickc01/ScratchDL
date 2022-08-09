using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Scratch_Downloader
{
	public class ProjectInfo
	{
		public class Author
		{
			public long id;
			public string username;
			public bool scratchteam;
			public Dictionary<string, string> history;
			public Profile profile;
		}

		public class Profile
		{
			public Dictionary<string, string> images;
		}

		public class ProjectHistory
		{
			public DateTime created;
			public DateTime modified;
			public DateTime? shared;
		}

		public class ProjectStats
		{
			public long views;
			public long loves;
			public long favorites;
			public long remixes;
		}

		public class ProjectRemix
		{
			public string parent;
			public string root;
		}

		public long id;
		public string title;
		public string description;
		public string instructions;
		public string visibility;
		[JsonProperty("public")]
		public bool is_public;
		public bool comments_allowed;
		public bool is_published;

		public Author author;
		public string image;
		public Dictionary<string, string> images;
		public ProjectHistory history;
		public ProjectStats stats;
		public ProjectRemix remix;
	}
}
