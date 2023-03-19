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
    public abstract class DownloadMode
    {
        public readonly MainWindowViewModel ViewModel;

        public DownloadMode(MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        /*public ScratchAPI API { get; private set; }

        public Action<double> SetProgress { get; private set; }

        public void Configure(ScratchAPI api, Action<double> setProgress)
        {
            API = api;
            SetProgress = setProgress;
        }*/

        public abstract Task Download(DownloadData data);

        public abstract Task Export(ExportData data);
    }
}
