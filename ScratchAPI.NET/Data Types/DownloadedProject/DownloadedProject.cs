using ScratchDL.Enums;

namespace ScratchDL
{
    public abstract class DownloadedProject
    {
        public string? Author;

        public DownloadedProject(string? author)
        {
            Author = author;
        }

        public abstract ProjectType Type { get; }
        public abstract Task Package(FileInfo destination);

        public async Task ExportProject(DirectoryInfo directory, string fileName)
        {
            await Package(new FileInfo(Helpers.PathAddBackslash(directory.FullName) + fileName + "." + Type.ToString().ToLower()));
        }
    }
}
