using ScratchDL.Enums;

namespace ScratchDL
{
    public class SB1Project : DownloadedProject
    {
        public override ProjectType Type => ProjectType.SB;
        public byte[] Data;

        public SB1Project(string? author, byte[] data) : base(author)
        {
            Data = data;
        }

        public override async Task Package(FileInfo destination)
        {
            if (!destination.Directory!.Exists)
            {
                destination.Directory.Create();
            }

            using (var fileStream = await Helpers.WaitTillFileAvailable(destination.FullName,FileMode.Create,FileAccess.Write))
            {
                await fileStream.WriteAsync(Data);
            }

            /*for (int i = 0; i < 100; i++)
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
            }*/
        }
    }
}
