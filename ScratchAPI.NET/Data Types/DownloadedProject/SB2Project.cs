using Scratch_Downloader.Enums;
using System.IO.Compression;

namespace Scratch_Downloader
{
    public class SB2Project : DownloadedProject
    {
        public override ProjectType Type => ProjectType.SB2;

        public List<SB2File> Files;

        public SB2Project(string? author, List<SB2File> files) : base(author)
        {
            Files = files;
        }

        public override void Package(FileInfo destination)
        {
            if (!destination.Directory!.Exists)
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
                    for (int i = 0; i < 100; i++)
                    {
                        try
                        {
                            destination.Delete();
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
}
