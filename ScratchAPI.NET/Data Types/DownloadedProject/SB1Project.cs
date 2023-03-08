using Scratch_Downloader.Enums;

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

            for (int i = 0; i < 100; i++)
            {
                try
                {
                    using (var fileStream = File.Create(destination.FullName))
                    {
                        fileStream.Write(Data);
                    }
                    break;
                }
                catch (Exception)
                {
                    if (i == 99)
                    {
                        throw;
                    }
                    continue;
                }
            }
        }
    }
}
