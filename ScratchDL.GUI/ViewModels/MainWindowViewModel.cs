using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using ReactiveUI;
using ScratchDL.GUI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScratchDL.GUI.ViewModels
{

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

        int _selectedModeIndex = 0;
        public int SelectedModeIndex
        {
            get => _selectedModeIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedModeIndex, value);
                OnModeSelection(value);
            }
        }

        public bool UILocked => currentUILocks.Count > 0;

        public bool LoggedIn => api.LoggedIn;

        bool _showDownloadProgressBar = true;
        public bool ShowDownloadProgressBar
        {
            get => _showDownloadProgressBar;
            set => this.RaiseAndSetIfChanged(ref _showDownloadProgressBar, value);
        }

        float _downloadProgress = 0f;
        public float DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
        }

        public List<DownloadMode> Modes;

        public List<ProjectEntry> ProjectEntries { get; private set; } = new List<ProjectEntry>();



        public MainWindowViewModel()
        {
            
        }

        public void OnModeSelection(int modeIndex)
        {
            Debug.WriteLine("Current Mode = " + modeIndex);
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

        public UILock LockUI()
        {
            var uiLock = new UILock(currentUILocks);
            this.RaisePropertyChanged(nameof(UILocked));
            return uiLock;
        }

        public bool UnlockUI(UILock lockID) => UnlockUI(lockID.ID);

        public bool UnlockUI(Guid lockID)
        {
            var removed = currentUILocks.Remove(lockID);
            this.RaisePropertyChanged(nameof(UILocked));
            return removed;
        }

        public ProjectEntry AddProjectEntry(long id, string name, string creator)
        {
            var entry = new ProjectEntry(true, id, name, creator);
            ProjectEntries.Add(entry);
            this.RaisePropertyChanged(nameof(ProjectEntries));
            return entry;
        }

        public bool RemoveProjectEntry(ProjectEntry entry)
        {
            var removed = ProjectEntries.Remove(entry);
            if (removed)
            {
                this.RaisePropertyChanged(nameof(ProjectEntries));
            }
            return removed;
        }

        public bool RemoveProjectEntry(long id)
        {
            var foundEntry = ProjectEntries.FirstOrDefault(e => e.ID == id);
            if (foundEntry != null)
            {
                return RemoveProjectEntry(foundEntry);
            }
            else
            {
                return false;
            }
        }
    }
}