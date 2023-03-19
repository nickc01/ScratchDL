﻿using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ReactiveUI;
using ScratchDL.GUI.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive;
using System.Reflection;
using System.Resources;
using System.Threading;
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



        bool _exportConsoleVisible = false;
        public bool ExportConsoleVisible
        {
            get => _exportConsoleVisible;
            set => this.RaiseAndSetIfChanged(ref _exportConsoleVisible, value);
        }

        string? _profileImage = null;
        public string? ProfileImage
        {
            get => _profileImage;
            set => this.RaiseAndSetIfChanged(ref _profileImage, value);
        }

        public AvaloniaList<string> ExportConsoleText { get; private set; } = new AvaloniaList<string>();
        public List<DownloadMode>? Modes;

        /*string _firstColumnName = "ID";
        public string FirstColumnName
        {
            get => _firstColumnName;
            set => _firstColumnName = value;
        }

        string _secondColumnName = "Name";
        public string SecondColumnName
        {
            get => _secondColumnName;
            set => _secondColumnName = value;
        }

        string _thirdColumnName = "Creator";
        public string ThirdColumnName
        {
            get => _thirdColumnName;
            set => _thirdColumnName = value;
        }*/

        public AvaloniaList<DownloadEntry> DownloadEntries { get; private set; } = new AvaloniaList<DownloadEntry>();

        public ICommand BeginDownloadCommand { get; private set; }
        public ICommand SelectAllCommand { get; private set; }
        public ICommand ExportProjectsCommand { get; private set; }
        public ICommand CloseExportConsoleCommand { get; private set; }
        public ICommand ClearDownloadGridCommand { get; private set; }

        DownloadMode? currentMode;

        public MainWindowViewModel()
        {
            BeginDownloadCommand = ReactiveCommand.CreateFromTask(DownloadProjects);
            SelectAllCommand = ReactiveCommand.Create(SelectAllEntries);
            ExportProjectsCommand = ReactiveCommand.CreateFromTask<string>(ExportProjects);
            CloseExportConsoleCommand = ReactiveCommand.Create(CloseExportConsole);
            ClearDownloadGridCommand = ReactiveCommand.Create(ClearDownloadGrid);
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

                    DownloadProfileImageAsync(api.ProfileLoginInfo!.user.id);
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
                UnlockUI(id);
            }
        }

        async Task DownloadProfileImageAsync(long userID)
        {
            ProfileImage = null;

            using var imgStream = await api.DownloadProfileImage(userID);

            ImageCache.AddImage(userID.ToString(), new Bitmap(imgStream));

            ProfileImage = userID.ToString();
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

        public void AddProjectEntry(DownloadEntry entry)
        {
            DownloadEntries.Add(entry);
            this.RaisePropertyChanged(nameof(DownloadEntries));
        }

        public DownloadEntry AddProjectEntry(long id, string name, string creator)
        {
            var entry = new DownloadEntry(true, id, name, creator);
            AddProjectEntry(entry);
            return entry;
        }

        public bool RemoveProjectEntry(DownloadEntry entry)
        {
            var removed = DownloadEntries.Remove(entry);
            if (removed)
            {
                this.RaisePropertyChanged(nameof(DownloadEntries));
            }
            return removed;
        }

        public bool RemoveProjectEntry(long id)
        {
            var foundEntry = DownloadEntries.FirstOrDefault(e => e.ID == id);
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
            var allSelected = DownloadEntries.AsParallel().All(e => e.Selected);
            Parallel.For(0, DownloadEntries.Count, i =>
            {
                DownloadEntries[i].Selected = !allSelected;
            });
            this.RaisePropertyChanged(nameof(DownloadEntries));
        }

        public async Task ExportProjects(string? folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || currentMode == null)
            {
                return;
            }

            DisplayDownloadError = false;

            var uiLock = LockUI();
            ExportConsoleText.Clear();
            this.RaisePropertyChanged(nameof(ExportConsoleText));
            ExportConsoleVisible = true;
            DownloadProgress = 0;
            ShowDownloadProgressBar = true;

            try
            {
                bool done = false;
                Exception? thrownException = null;

                ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
                ConcurrentQueue<double> progressQueue = new ConcurrentQueue<double>();

                async Task ExportThread()
                {
                    try
                    {
                        //currentMode.Configure(api, progressQueue.Enqueue);
                        var exportData = new ExportData(
                            api,
                            progressQueue.Enqueue,
                            new DirectoryInfo(folderPath),
                            DownloadEntries.Where(p => p.Selected).Select(p => p.ID),
                            messageQueue.Enqueue);
                        await currentMode.Export(exportData);
                        //await currentMode.Export(new DirectoryInfo(folderPath), DownloadEntries.Where(p => p.Selected).Select(p => p.ID), messageQueue.Enqueue);
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

                Task.Run(ExportThread);

                while (!done)
                {
                    await Task.Delay(50);
                    int newMessages = 0;
                    while (messageQueue.TryDequeue(out var message))
                    {
                        newMessages++;
                        ExportConsoleText.Add(message);
                    }
                    if (newMessages > 0)
                    {
                        this.RaisePropertyChanged(nameof(ExportConsoleText));
                    }
                    double currentProgress = -1;
                    while (progressQueue.TryDequeue(out var result))
                    {
                        currentProgress = result;
                    }
                    if (currentProgress > -1)
                    {
                        DownloadProgress = currentProgress;
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
                DownloadError = e.Message;
                DisplayDownloadError = true;
#if DEBUG
                throw;
#endif
            }
            finally
            {
                ShowDownloadProgressBar = false;
                DownloadProgress = 0;
                UnlockUI(uiLock);
            }
        }

        public void CloseExportConsole()
        {
            ExportConsoleVisible = false;
        }

        public void ClearDownloadGrid()
        {
            DownloadEntries.Clear();
            this.RaisePropertyChanged(nameof(DownloadEntries));
        }

        public async Task DownloadProjects()
        {
            if (Modes == null)
            {
                return;
            }
            currentMode = Modes[SelectedModeIndex];

            DownloadEntries.Clear();
            DisplayDownloadError = false;
            DownloadProgress = 0f;
            ShowDownloadProgressBar = true;
            ExportConsoleVisible = false;
            ExportConsoleText.Clear();
            this.RaisePropertyChanged(nameof(ExportConsoleText));
            var uiLock = LockUI();

            try
            {
                //currentMode.Configure(api, progress => DownloadProgress = Math.Clamp(progress, 0f, 100f));
                var downloadData = new DownloadData(
                    api,
                    progress => DownloadProgress = Math.Clamp(progress, 0f, 100f),
                    AddProjectEntry);
                await currentMode.Download(downloadData);
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