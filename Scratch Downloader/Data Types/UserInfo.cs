using System;
using System.Collections.Generic;

namespace Scratch_Downloader
{
	public class UserInfo
	{
		public class History
		{
			public DateTime? joined;
			public DateTime? login;
		}

		public class Profile
		{
			public long id;
			public Dictionary<string, string> images;
			public string status;
			public string bio;
			public string country;
		}

		public long id;
		public string username;
		public bool scratchteam;
		public History history;
		public Profile profile;

	}
}
