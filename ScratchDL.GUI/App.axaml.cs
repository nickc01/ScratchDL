using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScratchDL.GUI.ViewModels;
using ScratchDL.GUI.Views;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ScratchDL.GUI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = new MainWindowViewModel();

                var modeTypes = Assembly.GetAssembly(typeof(MainWindowViewModel))!.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && !t.ContainsGenericParameters && t != typeof(DownloadMode) && t.IsAssignableTo(typeof(DownloadMode)));

                List<DownloadMode> modes = new List<DownloadMode>();

                foreach (var type in modeTypes)
                {
                    modes.Add((DownloadMode)Activator.CreateInstance(type, vm)!);
                }

                vm.Modes = modes;

                var modeUITypes = Assembly.GetAssembly(typeof(MainWindowViewModel))!.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && !t.ContainsGenericParameters && t != typeof(IDownloadModeUI) && t.IsAssignableTo(typeof(IDownloadModeUI)));

                List<IDownloadModeUI> modeUIs = new List<IDownloadModeUI>();

                foreach (var type in modeUITypes)
                {
                    Type modeType;
                    try
                    {
                        modeType = type.BaseType!.GetGenericArguments().First();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{type.FullName} does not directly inherit from {IDownloadModeUI.GenericUIType}", e);
                    }

                    var mode = modes.FirstOrDefault(m => m.GetType().IsAssignableTo(modeType));

                    modeUIs.Add((IDownloadModeUI)Activator.CreateInstance(type, mode)!);
                }

                desktop.MainWindow = new MainWindow(vm, modeUIs);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}