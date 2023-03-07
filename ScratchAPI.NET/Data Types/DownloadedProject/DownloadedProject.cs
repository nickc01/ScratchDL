using Scratch_Downloader.Enums;
using System;
using System.IO;
using System.Text;

namespace Scratch_Downloader
{
    public abstract class DownloadedProject
	{
		public string? Author;

		public DownloadedProject(string? author)
        {
			Author = author;
        }

		public abstract ProjectType Type { get; }
		public abstract void Package(FileInfo destination);

		public void ExportProject(DirectoryInfo directory, string fileName)
		{
			Package(new FileInfo(ScratchAPI.PathAddBackslash(directory.FullName) + fileName + "." + Type.ToString().ToLower()));
		}
	}
}
