using System;
using System.Collections.Generic;
using System.Text;

namespace Scratch_Downloader
{
	public struct User
	{
		public string Username;
		public int ID;
		public string ThumbnailURL;
		public bool Admin;
	}

	public struct GalleryProjectInfoOLD
	{
		public int ID;
		public int ViewCount;
		public int FavoriteCount;
		public int RemixCount;
		public User Creator;
		public string Title;
		public bool IsPublished;
		public string DateCreated;
		public string ThumbnailURL;
		public string Visibility;
		public int LoveCount;
		public string DateModified;
		public string UncachedThumbnailURL;
		public string DateShared;
		public int CommentCount;
	}
}
