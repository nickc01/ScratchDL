using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchDL.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScratchDL.GUI.Views
{
    public partial class LoginWindow : Window
    {
        public static LoginWindow? Instance { get; private set; }

        public LoginWindow()
        {
            Instance = this;
            InitializeComponent();

            var loginButton = this.Find<Button>("login_button");
            loginButton.Click += OnLogin;
        }

        private void OnLogin(object? sender, RoutedEventArgs e)
        {
            Close();
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
