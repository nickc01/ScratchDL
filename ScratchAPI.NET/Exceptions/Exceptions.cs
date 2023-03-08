using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
		public readonly HttpStatusCode StatusCode;

		public LoginException(HttpStatusCode statusCode) 
		{ 
			StatusCode = statusCode; 
		}
		public LoginException(HttpStatusCode statusCode, string message) : base(message) 
		{
            StatusCode = statusCode;
        }
		public LoginException(HttpStatusCode statusCode, string message, Exception inner) : base(message, inner) 
		{
            StatusCode = statusCode;
        }
		protected LoginException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) 
		{

		}
	}


	[Serializable]
	public class ProjectDownloadException : Exception
	{
		public readonly long ProjectID;

		public ProjectDownloadException(long projectID) { ProjectID = projectID; }
		public ProjectDownloadException(long projectID, string message) : base(message) { ProjectID = projectID; }
		public ProjectDownloadException(long projectID, string message, Exception inner) : base(message, inner) { ProjectID = projectID; }
		protected ProjectDownloadException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

}
