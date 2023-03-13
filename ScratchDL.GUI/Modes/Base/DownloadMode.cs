using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
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

        public abstract Task Download(Action<ProjectEntry> addEntry);

        public abstract Task Export(IEnumerable<long> selectedIDs);
    }
}
