using Avalonia.Collections;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScratchDL.GUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, ILoginable
    {
        //Stores the active UI locks to prevent the user from doing any actions while something is happening
        private readonly HashSet<Guid> currentUILocks = new();

        //The Scratch API class for accessing the Scratch website
        private readonly ScratchAPI api = ScratchAPI.Create();


        /// <summary>
        /// The username of the user currently logged in
        /// </summary>
        public string LoggedInUser
        {
            get => _loggedInUser;
            set => this.RaiseAndSetIfChanged(ref _loggedInUser, value);
        }
        private string _loggedInUser = "Not Logged In";

        /// <summary>
        /// The selected <see cref="ProgramOption"/> index. 
        /// </summary>
        public int SelectedModeIndex
        {
            get => _selectedModeIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedModeIndex, value);
        }
        private int _selectedModeIndex = 0;

        /// <summary>
        /// Returns true if the UI is currently locked
        /// </summary>
        public bool UILocked => currentUILocks.Count > 0;

        /// <summary>
        /// Returns true if the app is currently logged into a Scratch user
        /// </summary>
        public bool LoggedIn => api.LoggedIn;

        /// <summary>
        /// If true, will show the progress bar in the UI
        /// </summary>
        public bool ShowProgressBar
        {
            get => _showProgressBar;
            set => this.RaiseAndSetIfChanged(ref _showProgressBar, value);
        }
        private bool _showProgressBar = false;

        /// <summary>
        /// The value of the progress bar between 0 - 100
        /// </summary>
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _ = this.RaiseAndSetIfChanged(ref _progressValue, value);
                Debug.WriteLine("Progress = " + _progressValue);
            }
        }
        private double _progressValue = 0f;

        /// <summary>
        /// If a download mode occurred, this string will display it in the UI if <see cref="DisplayErrorMessage"/> is set to true
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }
        private string _errorMessage = string.Empty;

        /// <summary>
        /// If set to true, will display <see cref="ErrorMessage"/> in the UI
        /// </summary>
        public bool DisplayErrorMessage
        {
            get => _displayErrorMessage;
            set => this.RaiseAndSetIfChanged(ref _displayErrorMessage, value);
        }
        private bool _displayErrorMessage = false;

        /// <summary>
        /// If set to true, will show the Export Console UI
        /// </summary>
        public bool ExportConsoleVisible
        {
            get => _exportConsoleVisible;
            set => this.RaiseAndSetIfChanged(ref _exportConsoleVisible, value);
        }
        private bool _exportConsoleVisible = false;

        /// <summary>
        /// If set, will attempt to display the profile image in the UI. 
        /// It will search the <see cref="ImageCache"/> for the image, so be sure to call <see cref="ImageCache.AddImage(string, Bitmap)"/> before setting this property
        /// </summary>
        public string? ProfileImage
        {
            get => _profileImage;
            set => this.RaiseAndSetIfChanged(ref _profileImage, value);
        }
        private string? _profileImage = null;

        /// <summary>
        /// The text to be displayed if <see cref="ExportConsoleVisible"/> is set to true
        /// </summary>
        public AvaloniaList<string> ExportConsoleText { get; private set; } = new AvaloniaList<string>();

        /// <summary>
        /// A list of all the different program options to select from. The UI can change the <see cref="SelectedModeIndex"/> to change which option is selected
        /// </summary>
        public List<ProgramOption>? Options;

        /// <summary>
        /// A list of all the downloaded entries. This can be updated by the currently selected <see cref="ProgramOption"/>
        /// </summary>
        public AvaloniaList<DownloadEntry> DownloadEntries { get; private set; } = new AvaloniaList<DownloadEntry>();

        /// <summary>
        /// The command run when the "Download" button is pressed. Used to begin the download process
        /// </summary>
        public ICommand BeginDownloadCommand { get; private set; }

        /// <summary>
        /// The command run when the "Select All" button is pressed. Used to select or deselect all the <see cref="DownloadEntries"/>
        /// </summary>
        public ICommand SelectAllCommand { get; private set; }

        /// <summary>
        /// The command run when the "Export" button is pressed. Used to export all the selected <see cref="DownloadEntries"/>
        /// </summary>
        public ICommand ExportCommand { get; private set; }

        /// <summary>
        /// The command run when the "Close" button is pressed. Used to close the export console by setting <see cref="ExportConsoleVisible"/> to false
        /// </summary>
        public ICommand CloseExportConsoleCommand { get; private set; }

        //public ICommand ClearDownloadGridCommand { get; private set; }

        private ProgramOption? usedOptions;

        public MainWindowViewModel()
        {
            BeginDownloadCommand = ReactiveCommand.CreateFromTask(DownloadAllEntriesAsync);
            SelectAllCommand = ReactiveCommand.Create(SelectAllEntries);
            ExportCommand = ReactiveCommand.CreateFromTask<string>(ExportAllEntriesAsync);
            CloseExportConsoleCommand = ReactiveCommand.Create(CloseExportConsole);
            //ClearDownloadGridCommand = ReactiveCommand.Create(ClearDownloadGrid);
        }

        /// <summary>
        /// Used to login to a scratch account. This is required in order to run the <see cref="Options.DownloadAllSharedProjectsFromUser"/> option
        /// </summary>
        /// <param name="username">The username of the account</param>
        /// <param name="password">The password of the account</param>
        public async Task Login(string username, string password)
        {
            UILock id = LockUI();
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

                    _ = DownloadProfileImageAsync(api.ProfileLoginInfo!.user.id);
                }
                catch (NoInternetException e)
                {
                    LoggedInUser = "No Internet Connection. Try Again";
                    Debug.WriteLine(e);
                }
                catch (LoginException e)
                {
                    LoggedInUser = "Invalid Credentials. Try Again";
                    Debug.WriteLine(e);
                }
            }
            finally
            {
                _ = UnlockUI(id);
            }
        }

        /// <summary>
        /// Downloads a user's profile image and display's it in the UI
        /// </summary>
        /// <param name="userID">The ID of the user</param>
        private async Task DownloadProfileImageAsync(long userID)
        {
            ProfileImage = null;

            using Stream imgStream = await api.DownloadProfileImage(userID);

            ImageCache.AddImage(userID.ToString(), new Bitmap(imgStream));

            ProfileImage = userID.ToString();
        }

        ~MainWindowViewModel() => api?.Dispose();

        /// <summary>
        /// Locks the UI so the user cannot interact with UI elements while the lock is aquired
        /// </summary>
        /// <returns>Returns a UI lock object</returns>
        public UILock LockUI()
        {
            UILock uiLock = new(currentUILocks);
            this.RaisePropertyChanged(nameof(UILocked));
            return uiLock;
        }

        /// <summary>
        /// Unlocks the UI so the user can interact with UI elements again
        /// </summary>
        /// <param name="lockID">The lock to release</param>
        /// <returns>Returns true if the lock was removed</returns>
        public bool UnlockUI(UILock lockID)
        {
            bool removed = currentUILocks.Remove(lockID.ID);
            this.RaisePropertyChanged(nameof(UILocked));
            return removed;
        }

        /// <summary>
        /// Adds an entry to the <see cref="DownloadEntries"/> list
        /// </summary>
        /// <param name="entry">The entry to add</param>
        public void AddDownloadEntry(DownloadEntry entry)
        {
            DownloadEntries.Add(entry);
            this.RaisePropertyChanged(nameof(DownloadEntries));
        }

        /// <summary>
        /// Adds an entry to the <see cref="DownloadEntries"/> list
        /// </summary>
        /// <param name="id">The id of the download</param>
        /// <param name="name">The name of the download</param>
        /// <param name="creator">The creator of the download</param>
        /// <returns>Returns the added download entry</returns>
        public DownloadEntry AddDownloadEntry(long id, string name, string creator)
        {
            DownloadEntry entry = new(true, id, name, creator);
            AddDownloadEntry(entry);
            return entry;
        }

        /// <summary>
        /// Removes an entry from the <see cref="DownloadEntries"/> list
        /// </summary>
        /// <param name="entry">The entry to remove</param>
        /// <returns>Returns true if the entry was removed</returns>
        public bool RemoveProjectEntry(DownloadEntry entry)
        {
            bool removed = DownloadEntries.Remove(entry);
            if (removed)
            {
                this.RaisePropertyChanged(nameof(DownloadEntries));
            }
            return removed;
        }

        /// <summary>
        /// Removes an entry from the <see cref="DownloadEntries"/> list
        /// </summary>
        /// <param name="id">The id of the entry to remove</param>
        /// <returns>Returns true if the entry was removed</returns>
        public bool RemoveProjectEntry(long id)
        {
            DownloadEntry? foundEntry = DownloadEntries.FirstOrDefault(e => e.ID == id);
            return foundEntry != null && RemoveProjectEntry(foundEntry);
        }

        /// <summary>
        /// Selects or deselects all entries in the <see cref="DownloadEntries"/> list
        /// </summary>
        public void SelectAllEntries()
        {
            bool allSelected = DownloadEntries.AsParallel().All(e => e.Selected);
            _ = Parallel.For(0, DownloadEntries.Count, i =>
            {
                DownloadEntries[i].Selected = !allSelected;
            });
            this.RaisePropertyChanged(nameof(DownloadEntries));
        }

        /// <summary>
        /// Exports all selected entries in the <see cref="DownloadEntries"/> list to a folder
        /// </summary>
        /// <param name="folderPath">The folder to export to</param>
        public async Task ExportAllEntriesAsync(string? folderPath)
        {
            //If no folder was specified or if no task was run yet, then quit the function
            if (string.IsNullOrEmpty(folderPath) || usedOptions == null)
            {
                return;
            }

            DisplayErrorMessage = false;

            UILock uiLock = LockUI();
            ExportConsoleText.Clear();
            this.RaisePropertyChanged(nameof(ExportConsoleText));
            ExportConsoleVisible = true;
            ProgressValue = 0;
            ShowProgressBar = true;

            try
            {
                bool done = false;
                Exception? thrownException = null;

                ConcurrentQueue<string> messageQueue = new();
                ConcurrentQueue<double> progressQueue = new();

                async Task ExportThread()
                {
                    try
                    {
                        ExportData exportData = new(
                            api,
                            progressQueue.Enqueue,
                            new DirectoryInfo(folderPath),
                            DownloadEntries.Where(p => p.Selected).Select(p => p.ID),
                            messageQueue.Enqueue);
                        await usedOptions.Export(exportData);
                    }
                    catch (Exception e)
                    {
                        thrownException = e;
                    }
                    finally
                    {
                        done = true;
                    }
                }

                _ = Task.Run(ExportThread);

                while (!done)
                {
                    await Task.Delay(50);
                    int newMessages = 0;
                    while (messageQueue.TryDequeue(out string? message))
                    {
                        newMessages++;
                        ExportConsoleText.Add(message);
                    }
                    if (newMessages > 0)
                    {
                        this.RaisePropertyChanged(nameof(ExportConsoleText));
                    }
                    double currentProgress = -1;
                    while (progressQueue.TryDequeue(out double result))
                    {
                        currentProgress = result;
                    }
                    if (currentProgress > -1)
                    {
                        ProgressValue = currentProgress;
                    }
                }

                if (thrownException != null)
                {
                    throw thrownException;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                ErrorMessage = e.Message;
                DisplayErrorMessage = true;
#if DEBUG
                throw;
#endif
            }
            finally
            {
                ShowProgressBar = false;
                ProgressValue = 0;
                UnlockUI(uiLock);
            }
        }

        public void CloseExportConsole()
        {
            ExportConsoleVisible = false;
        }

        public async Task DownloadAllEntriesAsync()
        {
            if (Options == null)
            {
                return;
            }
            usedOptions = Options[SelectedModeIndex];

            DownloadEntries.Clear();
            DisplayErrorMessage = false;
            ProgressValue = 0f;
            ShowProgressBar = true;
            ExportConsoleVisible = false;
            ExportConsoleText.Clear();
            this.RaisePropertyChanged(nameof(ExportConsoleText));
            UILock uiLock = LockUI();

            try
            {
                //currentMode.Configure(api, progress => DownloadProgress = Math.Clamp(progress, 0f, 100f));
                DownloadData downloadData = new(
                    api,
                    progress => ProgressValue = Math.Clamp(progress, 0f, 100f),
                    AddDownloadEntry);
                await usedOptions.Download(downloadData);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                ErrorMessage = e.Message;
                DisplayErrorMessage = true;
            }
            finally
            {
                _ = UnlockUI(uiLock);
                ShowProgressBar = false;
                ProgressValue = 0f;
            }
        }
    }
}