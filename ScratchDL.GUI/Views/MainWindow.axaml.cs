using Avalonia;
using Avalonia.Controls;
using DynamicData;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI.Views
{
    public partial class MainWindow : Window
    {
        public IProgramOptionView[] ModeUIs;

        public MainWindow() : this(new MainWindowViewModel(), new List<IProgramOptionView>()) { }

        public MainWindow(object dataContext, IEnumerable<IProgramOptionView> modeUIs)
        {
            InitializeComponent();

            DataContext = dataContext;

            ModeUIs = modeUIs.ToArray();

            mode_selection.SelectionChanged += SelectionChanged;
            mode_selection.Items = ModeUIs.Select(ui => ui.ModeObject?.Name ?? "UNKNOWN");
            mode_selection.SelectedIndex = 0;

            login_button.Click += DisplayLoginWindow;
            export_button.Click += ExportProjects;
        }

        void ExportProjects(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _ = ExportProjectsAsync();
        }

        private async Task ExportProjectsAsync()
        {
            var folderDialog = new OpenFolderDialog();
            var folderPath = await folderDialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(folderPath))
            {
                export_button.Command?.Execute(folderPath);
            }
        }

        private void DisplayLoginWindow(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var login = LoginWindow.Instance ?? new LoginWindow();
            if (login.DataContext == null)
            {
                login.DataContext = new LoginWindowViewModel(DataContext as ILoginable);
            }
            login.Show(this);
        }

        private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var modeUI = ModeUIs[mode_selection.SelectedIndex];

            mode_controls_section.Children.Clear();

            var descriptionBlock = new TextBlock();
            descriptionBlock.Text = modeUI.ModeObject.Description + '\n';

            mode_controls_section.Children.Add(descriptionBlock);

            MyDataGrid.Columns[0].Header = modeUI.Column1;
            MyDataGrid.Columns[0].Header = modeUI.Column2;
            MyDataGrid.Columns[0].Header = modeUI.Column3;

            modeUI.Setup(mode_controls_section);
        }

        /*protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Instance == this)
            {
                Instance = null;
            }
        }*/
    }
}