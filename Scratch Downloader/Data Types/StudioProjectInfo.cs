using System.Collections.Generic;

namespace Scratch_Downloader
{
    public class StudioProjectInfo
    {
		public long id;
		public string title;
		public string image;
		public long creator_id;
		public string username;
		public Dictionary<string, string> avatar;
		public long actor_id;

		public static explicit operator ProjectInfo(StudioProjectInfo info)
		{
			return new ProjectInfo
			{
				author = new ProjectInfo.Author
				{
					id = info.creator_id,
					username = info.username,
					scratchteam = false,
					profile = new ProjectInfo.Profile
					{
						images = info.avatar
					}
				},
				id = info.id,
				title = info.title,
				image = info.image
			};
		}

	}
}
