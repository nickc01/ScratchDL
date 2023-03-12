using Avalonia.Controls;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScratchDL.GUI.Views
{
    public partial class MainWindow : Window
    {
        ScratchAPI? api;

        public static MainWindow? Instance { get; private set; }

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

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            api = ScratchAPI.Create();

            var modeSelectionBox = this.FindControl<ComboBox>("mode_selection");

            modeSelectionBox.Items = Enum.GetNames(typeof(DownloadOptions)).Select(e => Prettify(e)).ToArray();
            modeSelectionBox.SelectedIndex = 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Instance == this)
            {
                Instance = null;
            }

            if (api != null)
            {
                api.Dispose();
                api = null;
            }
        }
    }
}