using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{

	[Serializable]
	public class NoInternetException : Exception
	{
		public NoInternetException() { }
		public NoInternetException(string message) : base(message) { }
		public NoInternetException(string message, Exception inner) : base(message, inner) { }
		protected NoInternetException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}



	[Serializable]
	public class LoginException : Exception
	{
		public LoginException() { }
		public LoginException(string message) : base(message) { }
		public LoginException(string message, Exception inner) : base(message, inner) { }
		protected LoginException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

}
