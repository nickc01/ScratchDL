using Avalonia.Collections;
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
                //OnModeSelection(value);
            }
        }

        public bool UILocked => currentUILocks.Count > 0;

        public bool LoggedIn => api.LoggedIn;

        bool _showDownloadProgressBar = false;
        public bool ShowDownloadProgressBar
        {
            get => _showDownloadProgressBar;
            set => this.RaiseAndSetIfChanged(ref _showDownloadProgressBar, value);
        }

        double _downloadProgress = 0f;
        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadProgress, value);
                Debug.WriteLine("Progress = " + _downloadProgress);
            }
        }

        string _downloadError = string.Empty;
        public string DownloadError
        {
            get => _downloadError;
            set => this.RaiseAndSetIfChanged(ref _downloadError, value);
        }

        bool _displayDownloadError = false;
        public bool DisplayDownloadError
        {
            get => _displayDownloadError;
            set => this.RaiseAndSetIfChanged(ref _displayDownloadError, value);
        }

        public List<DownloadMode> Modes;

        public AvaloniaList<ProjectEntry> ProjectEntries { get; private set; } = new AvaloniaList<ProjectEntry>();

        public ICommand BeginDownloadCommand { get; private set; }
        public ICommand SelectAllCommand { get; private set; }

        public MainWindowViewModel()
        {
            BeginDownloadCommand = ReactiveCommand.CreateFromTask(BeginDownloadData);
            SelectAllCommand = ReactiveCommand.Create(SelectAllEntries);
        }

        /*public void OnModeSelection(int modeIndex)
        {

        }*/

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

        public void AddProjectEntry(ProjectEntry entry)
        {
            Debug.WriteLine("ProjecT Added");
            ProjectEntries.Add(entry);
            this.RaisePropertyChanged(nameof(ProjectEntries));
        }

        public ProjectEntry AddProjectEntry(long id, string name, string creator)
        {
            var entry = new ProjectEntry(true, id, name, creator);
            AddProjectEntry(entry);
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

        public void SelectAllEntries()
        {
            var allSelected = ProjectEntries.AsParallel().All(e => e.Selected);
            Debug.WriteLine("All Selected = " + allSelected);
            Parallel.For(0, ProjectEntries.Count, i =>
            {
                ProjectEntries[i].Selected = !allSelected;
            });
            this.RaisePropertyChanged(nameof(ProjectEntries));
        }

        public async Task BeginDownloadData()
        {
            var selectedMode = Modes[SelectedModeIndex];

            ProjectEntries.Clear();
            DisplayDownloadError = false;
            DownloadProgress = 0f;
            ShowDownloadProgressBar = true;
            var uiLock = LockUI();

            try
            {
                await selectedMode.Download(api, entry => AddProjectEntry(entry), progress => DownloadProgress = Math.Clamp(progress, 0f, 100f));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                DownloadError = e.Message;
                DisplayDownloadError = true;
            }
            finally
            {
                UnlockUI(uiLock);
                ShowDownloadProgressBar = false;
                DownloadProgress = 0f;
            }
        }
    }
}