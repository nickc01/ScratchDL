using Avalonia.Controls;
using ScratchDL.GUI.ViewModels;
using System;
using System.Linq;

namespace ScratchDL.GUI
{
    public interface IProgramOptionView
    {
        static Type? _genericUIType;
        public static Type GenericUIType => _genericUIType ??= typeof(MainWindowViewModel).GetNestedTypes().First();

        ProgramOption ModeObject { get; }

        Type ModeType { get; }

        void Setup(StackPanel controlPanel);

        string Column1 { get; }
        string Column2 { get; }
        string Column3 { get; }
    }
}
