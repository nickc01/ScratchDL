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

                var modeTypes = Assembly.GetAssembly(typeof(MainWindowViewModel))!.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && !t.ContainsGenericParameters && t != typeof(ProgramOption) && t.IsAssignableTo(typeof(ProgramOption)));

                List<ProgramOption> modes = new List<ProgramOption>();

                foreach (var type in modeTypes)
                {
                    modes.Add((ProgramOption)Activator.CreateInstance(type, vm)!);
                }

                List<ProgramOption> sortedModes = new List<ProgramOption>();

                vm.Options = sortedModes;

                var modeUITypes = Assembly.GetAssembly(typeof(MainWindowViewModel))!.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && !t.ContainsGenericParameters && t != typeof(IProgramOptionView) && t.IsAssignableTo(typeof(IProgramOptionView)));

                List<IProgramOptionView> modeUIs = new List<IProgramOptionView>();

                foreach (var type in modeUITypes)
                {
                    Type modeType;
                    try
                    {
                        modeType = type.BaseType!.GetGenericArguments().First();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{type.FullName} does not directly inherit from {IProgramOptionView.GenericUIType}", e);
                    }

                    var mode = modes.FirstOrDefault(m => m.GetType().IsAssignableTo(modeType));

                    if (mode != null)
                    {
                        modeUIs.Add((IProgramOptionView)Activator.CreateInstance(type, mode)!);
                        sortedModes.Add(mode);
                    }
                }

                desktop.MainWindow = new MainWindow(vm, modeUIs);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}