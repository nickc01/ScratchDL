using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Scratch_Downloader
{
	public enum ProjectType
	{
		SB,
		SB2,
		SB3
	}

	public abstract class DownloadedProject
	{
		public string Author;

		public abstract ProjectType Type { get; }
		public abstract void Package(FileInfo destination);

		public void ExportProject(DirectoryInfo directory, string fileName)
		{
			Package(new FileInfo(Utilities.PathAddBackslash(directory.FullName) + fileName + "." + Type.ToString().ToLower()));
		}
	}

	public class SB1Project : DownloadedProject
	{
		public override ProjectType Type => ProjectType.SB;
		public byte[] Data;

		public override void Package(FileInfo destination)
		{
			if (!destination.Directory.Exists)
			{
				destination.Directory.Create();
			}
			using (var fileStream = File.Create(destination.FullName))
			{
				fileStream.Write(Data);
			}
		}
	}

	public class SB2File
	{
		public string Path;
		public byte[] Data;

		public class Comparer : IComparer<SB2File>
		{
			public static Comparer Default = new Comparer();
			public int Compare([AllowNull] SB2File x, [AllowNull] SB2File y)
			{
				if (x.Path == "project.json")
				{
					return -1;
				}
				else if (y.Path == "project.json")
				{
					return 1;
				}

				return Comparer<string>.Default.Compare(x.Path,y.Path);
			}
		}
	}

	public class SB2Project : DownloadedProject
	{
		public override ProjectType Type => ProjectType.SB2;

		public List<SB2File> Files = new List<SB2File>();
		//public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

		public override void Package(FileInfo destination)
		{
			if (!destination.Directory.Exists)
			{
				destination.Directory.Create();
			}
			if (Files.Count == 1 && Files[0].Path == "BINARY.sb2")
			{
				//DUMP FILE DIRECTLY TO DESTINATION
				using (var fileStream = File.Create(destination.FullName))
				{
					fileStream.Write(Files[0].Data);
				}
			}
			else
			{
				//PACK MULTIPLE FILES AS A ZIP FILE, THEN DUMP TO DESTINATION
				if (destination.Exists)
				{
					destination.Delete();
				}
				using (var fileStream = File.Create(destination.FullName))
				{
					using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
					{
						foreach (var file in Files)
						{
							var entry = zipArchive.CreateEntry(file.Path);
							using (var entryStream = entry.Open())
							{
								entryStream.Write(file.Data);
							}
						}
					}
				}
			}
		}
	}

	public class SB3Project : DownloadedProject
	{
		public class SB3File
		{
			public string path;
			public byte[] data;

			public class Comparer : IComparer<SB3File>
			{
				public static Comparer Default = new Comparer();
				public int Compare([AllowNull] SB3File x, [AllowNull] SB3File y)
				{
					if (x.path == "project.json")
					{
						return -1;
					}
					else if (y.path == "project.json")
					{
						return 1;
					}

					return Comparer<string>.Default.Compare(x.path, y.path);
				}
			}
		}

		public class SB3JTokenDistict : IEqualityComparer<JToken>
		{
			public bool Equals(JToken x, JToken y)
			{
				return x.Value<string>("assetId") == y.Value<string>("assetId");
			}

			public int GetHashCode([DisallowNull] JToken obj)
			{
				return obj.Value<string>("assetId")?.GetHashCode() ?? 0;
			}
		}

		public List<SB3File> Files = new List<SB3File>();

		public override ProjectType Type => ProjectType.SB3;

		public override void Package(FileInfo destination)
		{
			if (!destination.Directory.Exists)
			{
				destination.Directory.Create();
			}
			if (Files.Count == 1 && Files[0].path == "BINARY.sb2")
			{
				//DUMP FILE DIRECTLY TO DESTINATION
				using (var fileStream = File.Create(destination.FullName))
				{
					fileStream.Write(Files[0].data);
				}
			}
			else
			{
				//PACK MULTIPLE FILES AS A ZIP FILE, THEN DUMP TO DESTINATION
				if (destination.Exists)
				{
					destination.Delete();
				}
				using (var fileStream = File.Create(destination.FullName))
				{
					using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
					{
						foreach (var file in Files)
						{
							var entry = zipArchive.CreateEntry(file.path);
							using (var entryStream = entry.Open())
							{
								entryStream.Write(file.data);
							}
						}
					}
				}
			}
		}
	}
}
