using Avalonia.Controls.Primitives;
using ReactiveUI;
using Scratch_Downloader;
using ScratchDL.GUI.Interfaces;
using ScratchDL.GUI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Threading.Tasks;

namespace ScratchDL.GUI.ViewModels
{
    public record class ProjectEntry
    (
        bool Selected,
        long ID,
        string Name,
        string Creator
    );


    public class UILock : IDisposable
    {
        public readonly Guid ID = Guid.NewGuid();

        readonly HashSet<Guid> _sourceSet;

        public UILock(HashSet<Guid> sourceSet)
        {
            _sourceSet = sourceSet;
            sourceSet.Add(ID);
        }

        public void Dispose()
        {
            _sourceSet.Remove(ID);
        }
    }

    public class MainWindowViewModel : ViewModelBase, ILoginable
    {
        HashSet<Guid> currentUILocks = new();

        ScratchAPI api = ScratchAPI.Create();

        string _loggedInUser = "Not Logged In";
        public string LoggedInUser
        {
            get => _loggedInUser;
            set => this.RaiseAndSetIfChanged(ref _loggedInUser, value);
        }

        public bool UILocked => currentUILocks.Count > 0;
        public bool LoggedIn => api.LoggedIn;

        public List<ProjectEntry> ProjectEntries { get; private set; } = new List<ProjectEntry>(); /*=> new List<ProjectEntry>
        {
            new ProjectEntry(false,8062448201,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448202,"Ropes","WingDingWarrior89"),
            new ProjectEntry(false,8062448203,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448204,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482011,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448205,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482012,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448206,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482013,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448207,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482014,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448208,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482015,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,8062448209,"Ropes","WingDingWarrior89"),new ProjectEntry(false,80624482016,"Ropes","WingDingWarrior89"),
            new ProjectEntry(true,80624482010,"Ropes","WingDingWarrior89")
        };*/

        public void DisplayLoginWindow()
        {
            var login = LoginWindow.Instance ?? new LoginWindow();
            if (login.DataContext == null)
            {
                login.DataContext = new LoginWindowViewModel(this);
            }
            login.Show(MainWindow.Instance);
        }

        public async Task Login(string username, string password)
        {
            var id = LockUI();
            try
            {
                if (api.LoggedIn)
                {
                    api.Logout();
                }

                LoggedInUser = "Logging In...";

                try
                {
                    await api.Login(username, password);

                    LoggedInUser = api.ProfileLoginInfo!.user.username;
                }
                catch (NoInternetException e)
                {
                    LoggedInUser = "No Internet Connection. Try Again";
                    Debug.WriteLine(e);
                }
                catch (LoginException e)
                {
                    LoggedInUser = "Invalid Credentials. Try Again";
                }
            }
            finally
            {
                UnlockUI(id);
            }
        }

        ~MainWindowViewModel() => api?.Dispose();

        public UILock LockUI() => new UILock(currentUILocks);

        public bool UnlockUI(UILock lockID) => UnlockUI(lockID.ID);

        public bool UnlockUI(Guid lockID) => currentUILocks.Remove(lockID);
    }
}