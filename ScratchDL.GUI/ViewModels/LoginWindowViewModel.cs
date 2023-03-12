using Avalonia.Interactivity;
using ReactiveUI;
using ScratchDL.GUI.Views;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScratchDL.GUI.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        public readonly ILoginable LoginObject;


        string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public ICommand LoginCommand { get; private set; }

        public LoginWindowViewModel(ILoginable loginObject)
        {
            LoginObject = loginObject;
            LoginCommand = ReactiveCommand.CreateFromTask(Login);
        }

        public async Task Login()
        {
            if (LoginObject != null)
            {
                await LoginObject.Login(Username, Password);
            }
        }
    }
}