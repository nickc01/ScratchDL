using System;
using System.Collections.Generic;

namespace ScratchDL
{
	public record class User
	(
		long id,
		string username,
		bool scratchteam,
		User.History history,
		User.Profile profile
	)
	{
		public record class History
		(
			DateTime? joined,
			DateTime? login
		);

		public record class Profile
		(
			long id,
			Dictionary<string, string> images,
			string status,
			string bio,
			string country
		);
	}
}
