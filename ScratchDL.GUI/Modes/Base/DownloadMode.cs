using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public abstract class DownloadMode
    {
        public readonly MainWindowViewModel ViewModel;

        public DownloadMode(MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract Task Download(ScratchAPI api, Action<ProjectEntry> addEntry, Action<double> setProgress);

        public abstract Task Export(DirectoryInfo folderPath, IEnumerable<long> selectedIDs);
    }
}
