using System.Collections.Generic;

namespace Scratch_Downloader
{
    public class SB2File
	{
		public string Path;
		public byte[] Data;

		public SB2File(string path, byte[] data)
        {
			Path = path;
			Data = data;
        }

		public class Comparer : IComparer<SB2File>
		{
			public static Comparer Default = new Comparer();
			public int Compare(SB2File? x,SB2File? y)
			{
				if (x?.Path == "project.json")
				{
					return -1;
				}
				else if (y?.Path == "project.json")
				{
					return 1;
				}

				return Comparer<string>.Default.Compare(x?.Path,y?.Path);
			}
		}
	}
}
