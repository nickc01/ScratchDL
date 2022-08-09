using System;

namespace Scratch_Downloader
{
	public record class LoginInfo
	{
		public record class UserInfo
		{
			public int id;
			public bool banned;
			public string username;
			public string token;
			public string thumbnailUrl;
			public DateTime dateJoined;
			public string email;
		}

		public record class Permissions
		{
			public bool admin;
			public bool scratcher;
			public bool new_scratcher;
			public bool social;
			public bool educator;
			public bool educator_invitee;
			public bool student;
		}

		public record class Flags
		{
			public bool must_reset_password;
			public bool must_complete_registration;
			public bool has_outstanding_email_confirmation;
			public bool show_welcome;
			public bool confirm_email_banner;
			public bool unsupported_browser_banner;
			public bool project_comments_enabled;
			public bool gallery_comments_enabled;
			public bool userprofile_comments_enabled;
		}

		public UserInfo user;
		public Permissions permissions;
		public Flags flags;
	}
}
