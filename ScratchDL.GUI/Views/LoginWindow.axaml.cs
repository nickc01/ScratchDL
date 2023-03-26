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

            login_button.Click += OnLogin;
            username_field.KeyDown += LoginOnEnter;
            password_field.KeyDown += LoginOnEnter;
        }

        private void LoginOnEnter(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter && !string.IsNullOrWhiteSpace(username_field.Text) && !string.IsNullOrWhiteSpace(password_field.Text))
            {
                login_button.Command?.Execute(null);
                login_button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
