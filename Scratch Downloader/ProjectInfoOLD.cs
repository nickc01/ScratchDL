using System;
using System.Collections.Generic;
using System.Text;

namespace Scratch_Downloader
{
	public struct ProjectInfoOLD
	{
		public int ID;
		public string Title;
		public string Description;
		public string Instructions;
		public string Visibility;
		public bool Public;
		public bool CommentsAllowed;
		public bool IsPublished;
		//public Dictionary<string, string> History;
		public string DateCreated;
		public string DateModified;
		public string DateShared;

		public int Views;
		public int Loves;
		public int Favorites;
		public int Remixes;
		public int AuthorID;
		public string username;
		public string ProjectImageURL;

		public int RemixParentProjectID;
		public int RemixRootProjectID;

	}
}
