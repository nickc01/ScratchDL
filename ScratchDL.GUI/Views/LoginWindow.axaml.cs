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

            var usernameField = this.Find<TextBox>("username_field");
            usernameField.KeyDown += LoginOnEnter;

            var passwordField = this.Find<TextBox>("password_field");
            passwordField.KeyDown += LoginOnEnter;
        }

        private void LoginOnEnter(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter && !string.IsNullOrWhiteSpace(username_field.Text) && !string.IsNullOrWhiteSpace(password_field.Text))
            {
                var loginButton = this.Find<Button>("login_button");
                loginButton.Command?.Execute(null);
                loginButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
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
