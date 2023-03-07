using System;

namespace Scratch_Downloader
{
	public record class LoginInfo (
		LoginInfo.UserInfo user,
		LoginInfo.Permissions permissions,
		LoginInfo.Flags flags
	)
	{
		public record class UserInfo
		(
			int id,
			bool banned,
			string username,
			string token,
			string thumbnailUrl,
			DateTime dateJoined,
			string email
		);

		public record class Permissions
		(
			bool admin,
			bool scratcher,
			bool new_scratcher,
			bool social,
			bool educator,
			bool educator_invitee,
			bool student
		);

		public record class Flags
		(
			bool must_reset_password,
			bool must_complete_registration,
			bool has_outstanding_email_confirmation,
			bool show_welcome,
			bool confirm_email_banner,
			bool unsupported_browser_banner,
			bool project_comments_enabled,
			bool gallery_comments_enabled,
			bool userprofile_comments_enabled
		);
	}
}
