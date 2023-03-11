using Avalonia.Controls;
using Scratch_Downloader;
using System;

namespace ScratchDL.GUI.Views
{
    public partial class MainWindow : Window
    {
        ScratchAPI? api;



        public static MainWindow? Instance { get; private set; }
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            api = ScratchAPI.Create();
        }

        public void Login(string username, string password)
        {

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