using ScratchDL.Enums;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public abstract class ProgramOption
    {
        public readonly MainWindowViewModel ViewModel;

        public ProgramOption(MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract Task Download(DownloadData data);

        public abstract Task Export(ExportData data);
    }
}
