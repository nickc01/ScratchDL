using System;

namespace ScratchDL.GUI
{
    public record class DownloadData(
        ScratchAPI API,
        Action<double> SetProgress,
        Action<DownloadEntry> AddEntry)
    {

    }
}
