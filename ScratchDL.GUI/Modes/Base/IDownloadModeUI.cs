using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Linq;

namespace ScratchDL.GUI
{
    public interface IDownloadModeUI
    {
        static Type? _genericUIType;
        public static Type GenericUIType => _genericUIType ??= typeof(MainWindowViewModel).GetNestedTypes().First();

        DownloadMode ModeObject { get; }

        Type ModeType { get; }

        void Setup(StackPanel controlPanel);
    }
}
