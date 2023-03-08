using Scratch_Downloader.Enums;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;

namespace Scratch_Downloader
{
    public class SB3Project : DownloadedProject
    {
        public class SB3File
        {
            public string Path;
            public byte[] Data;

            public SB3File(string path, byte[] data)
            {
                Path = path;
                Data = data;
            }

            public class Comparer : IComparer<SB3File>
            {
                public static Comparer Default = new Comparer();
                public int Compare([AllowNull] SB3File x, [AllowNull] SB3File y)
                {
                    if (x?.Path == "project.json")
                    {
                        return -1;
                    }
                    else if (y?.Path == "project.json")
                    {
                        return 1;
                    }

                    return Comparer<string>.Default.Compare(x?.Path, y?.Path);
                }
            }
        }

        public class SB3JsonElementDistict : IEqualityComparer<JsonElement>
        {
            public bool Equals(JsonElement x, JsonElement y)
            {
                if (x.TryGetProperty("assetID", out var xID) && y.TryGetProperty("assetId", out var yID))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(JsonElement obj)
            {
                if (obj.TryGetProperty("assetId", out var ID))
                {
                    return ID.GetString()?.GetHashCode() ?? 0;
                }
                else
                {
                    return 0;
                }
            }
        }

        public List<SB3File> Files;

        public SB3Project(string? author, List<SB3File> files) : base(author)
        {
            Files = files;
        }

        public override ProjectType Type => ProjectType.SB3;

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
