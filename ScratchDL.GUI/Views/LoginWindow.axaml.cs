using Avalonia.Controls;
using System;

namespace ScratchDL.GUI.Views
{
    public partial class LoginWindow : Window
    {
        public static LoginWindow? Instance { get; private set; }

        public LoginWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
