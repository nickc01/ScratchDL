using Avalonia.Controls;
using DynamicData;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScratchDL.GUI.Views
{
    public partial class MainWindow : Window
    {
        //public static MainWindow? Instance { get; private set; }

        public IDownloadModeUI[] ModeUIs;

        ComboBox modeSelectionBox;

        static string Prettify(string input)
        {
            StringBuilder builder = new (input);
            for (int index = 0; index < builder.Length - 1; index++)
            {
                if (char.IsLower(builder[index]) && char.IsUpper(builder[index + 1]))
                {
                    builder.Insert(index + 1, ' ');
                }
            }
            return builder.ToString();
        }

        public MainWindow() : this(new MainWindowViewModel(), new List<IDownloadModeUI>()) { }

        public MainWindow(object dataContext, IEnumerable<IDownloadModeUI> modeUIs)
        {
            //Instance = this;
            InitializeComponent();

            DataContext = dataContext;

            ModeUIs = modeUIs.ToArray();

            modeSelectionBox = this.FindControl<ComboBox>("mode_selection");

            modeSelectionBox.SelectionChanged += SelectionChanged;

            modeSelectionBox.Items = ModeUIs.Select(ui => ui.ModeObject?.Name ?? "UNKNOWN");
            modeSelectionBox.SelectedIndex = 0;

            var modeControlsSection = this.FindControl<StackPanel>("mode_controls_section");

            var loginButton = this.FindControl<Button>("login_button");
            loginButton.Click += DisplayLoginWindow;
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
            Debug.WriteLine("Selection Changed to = " + modeSelectionBox.SelectedIndex);
            //TODO - RUN THE UI MODE TO SETUP GUI, and also run any code in the View Model too

            var modeUI = ModeUIs[modeSelectionBox.SelectedIndex];

            var modeControlsSection = this.FindControl<StackPanel>("mode_controls_section");
            modeControlsSection.Children.Clear();


            modeUI.Setup(modeControlsSection);
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