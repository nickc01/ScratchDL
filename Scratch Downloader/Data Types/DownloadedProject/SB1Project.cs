using Scratch_Downloader.Enums;
using System.IO;

namespace Scratch_Downloader
{
    public class SB1Project : DownloadedProject
	{
		public override ProjectType Type => ProjectType.SB;
		public byte[] Data;

		public SB1Project(string? author, byte[] data) : base(author)
        {
			Data = data;
        }

		public override void Package(FileInfo destination)
		{
			if (!destination.Directory!.Exists)
			{
				destination.Directory.Create();
			}
			using (var fileStream = File.Create(destination.FullName))
			{
				fileStream.Write(Data);
			}
		}
	}
}
