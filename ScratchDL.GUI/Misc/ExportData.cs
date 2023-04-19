using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScratchDL.GUI
{
    public record class ExportData(
        ScratchAPI API,
        Action<double> SetProgress,
        DirectoryInfo FolderPath, 
        IEnumerable<long> SelectedIDs, 
        Action<string> WriteToConsole)
    {

    }
}
