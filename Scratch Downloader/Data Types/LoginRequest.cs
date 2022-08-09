using System.Text;

namespace Scratch_Downloader
{
	public class LoginRequest
	{
		public string username;
		public string password;
		public string csrftoken;
		public string csrfmiddlewaretoken;
		public string captcha_challenge = "";
		public string captcha_response = "";
		public bool useMessages = true;
		public string timezone = "America/New_York";
	}
}
