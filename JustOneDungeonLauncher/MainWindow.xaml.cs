using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibGit2Sharp;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace JustOneDungeonLauncher
{

    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        ServerOffline,
        NotLoggedIn
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        private const string VersionFileLink = "https://dl.dropboxusercontent.com/s/kzvhqeoaiktrq0s/Version.txt?dl=0";
        private const string PatchNotesLink = "https://dl.dropboxusercontent.com/s/9urtsbh7lwp7cek/Patchnotes.txt?dl=0";
        private const string isOnline = "https://dl.dropboxusercontent.com/s/fyufsgafqu6t7y7/isOnline.txt?dl=0";

        private string appDataRoaming = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        private string appData = "";
        private string saveDataPath = "";
        private string versionFile = "";
        
        private string gameExe = "";
        private bool isUpdating;

        private string RepoString = "https://github.com/Midpls/JustOneDungeonBuild";
        private string RepoPath;
        
        private LauncherStatus _status;
    
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Tavern is being build...";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Getting the dungeon enemies ready...";
                        break;
                    case LauncherStatus.ServerOffline:
                        PlayButton.Content = "Server Not Reachable - Retry";
                        break;
                    case LauncherStatus.NotLoggedIn:
                        PlayButton.Content = "Not logged in - Retry";
                        break;
                }
            }
        }
        
        
        
        public MainWindow()
        {
            try
            { 
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void CheckForUpdates()
        {
        
            isUpdating = true;
            if (File.Exists(versionFile) && !File.Exists(gameExe))
            {
                File.Delete(versionFile);
                InstallGameFiles(false, Version.zero);
                return;
            }
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(VersionFileLink));

                    if (onlineVersion.isDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        isUpdating = false;
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
            
        }

        private void ShowPatchNotesAsync()
        {
            WebClient client = new WebClient();
            var Task = System.Threading.Tasks.Task.Run(() => client.DownloadStringTaskAsync(PatchNotesLink));
            Task.Wait();
            PatchNotesText.Text = Task.Result;

        }
        
        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(VersionFileLink));
                }

                currentVersion = _onlineVersion;

                
                if (Directory.Exists(RepoPath))
                {
                    ClearReadOnly(new DirectoryInfo(RepoPath));
                    Directory.Delete(RepoPath, true);
                }
                
                Directory.CreateDirectory(RepoPath);
                
                DownloadGame();
                //if download of webclient finishes call the following function

            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing files: {ex}");
            }
        }
        
        private void ClearReadOnly(DirectoryInfo parentDirectory)
        {
            if(parentDirectory != null)
            {
                parentDirectory.Attributes = FileAttributes.Normal;
                foreach (FileInfo fi in parentDirectory.GetFiles())
                {
                    fi.Attributes = FileAttributes.Normal;
                }
                foreach (DirectoryInfo di in parentDirectory.GetDirectories())
                {
                    ClearReadOnly(di);
                }
            }
        }

        private async void DownloadGame()
        {
            await Task.Run( () => Repository.Clone(RepoString, RepoPath));
            DownloadGameCompletedCallback();
        }
        

        private Version currentVersion;
        private void DownloadGameCompletedCallback()
        {
            try
            {
                string onlineVersion = currentVersion.ToString();
              
                File.WriteAllText(versionFile, onlineVersion);
                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
                isUpdating = false;
                currentVersion = Version.zero;

            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"RESTART THE APP AND RUN IT AS ADMINISTRATOR! {ex}");
            }
        }
        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            try
            {
                appData = Path.Combine(Directory.GetParent(appDataRoaming).ToString());
                //set the name to wedentertainment of the new directory and put the string of the full path to the save data
                saveDataPath = Directory.CreateDirectory(Path.Combine(appData, @"Wedentertainment")).ToString();
                versionFile = Path.Combine(saveDataPath, "Version.txt");
                RepoPath = Path.Combine(saveDataPath, "Build_dir");
                gameExe = Path.Combine(saveDataPath, "Build_dir", "Just one Dungeon.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
            }

            ShowPatchNotesAsync();
            CheckForUpdates();
        }

        private void PlayButton_OnClick(object sender, RoutedEventArgs e)
        {
            WebClient isOnlineChecker = new WebClient();
            if (isOnlineChecker.DownloadString(isOnline) != "True")
            {
                Status = LauncherStatus.ServerOffline;
                return;
            }

            if (Status == LauncherStatus.NotLoggedIn) return;
            if (isUpdating) return;
            if (!File.Exists(gameExe))
            {
                Status = LauncherStatus.failed;
            }
            if (Status == LauncherStatus.ready)
            {
                if (!isLoggedIn)
                {
                    Status = LauncherStatus.NotLoggedIn;
                    return;
                }
                ProcessStartInfo startinfo = new ProcessStartInfo(gameExe);
                startinfo.WorkingDirectory = Path.Combine(saveDataPath);
                Process.Start(startinfo);
                
                Close();
            }
            else if (Status == LauncherStatus.failed || File.Exists(versionFile))
            {
                CheckForUpdates();
            }
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        

        private void LogInMenu_Click(object sender, RoutedEventArgs e)
        {
            ChangeLoginMenu();
        }

        private void ChangeLoginMenu()
        {
            OptionsPanel.IsEnabled = !OptionsPanel.IsEnabled;
            if (OptionsPanel.IsEnabled)
            {
                OptionsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                OptionsPanel.Visibility = Visibility.Hidden;
            }
        }

        private bool isLoggedIn;

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            if (UserNameInputText.Text != "" && UserNameInputText.Text.Length <= 13)
            {
                if (_status == LauncherStatus.NotLoggedIn)
                {
                    Status = LauncherStatus.ready;
                }

                LoggedInText.Text = "Current user: " + UserNameInputText.Text + " (EU)"; 
                isLoggedIn = true;
                ChangeLoginMenu();
            } 
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            if (Status == LauncherStatus.ready)
            {
                Status = LauncherStatus.NotLoggedIn;
                isLoggedIn = false;
                LoggedInText.Text = "User: None";
                ChangeLoginMenu();
            }
        }

        private bool errorIsActive;
        /// <summary>
        /// Shows an error according to which one in the middle of the screen as red text
        /// </summary>
        /// <param name="which">0 is not logged in error</param>
        private void initErrorMessage(int which)
        {
            StopLastError();
            errorIsActive = true;
            ErrorMessage.Visibility = Visibility.Visible;
            switch (which)
            {
                case 0: Status = LauncherStatus.NotLoggedIn;
                    ErrorMessage.Text = "Not logged in yet!";
                    break;
            }
        }

        private void StopLastError()
        {
            ErrorMessage.Visibility = Visibility.Hidden;
            errorIsActive = false;
        }
        
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);
        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if (_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool isDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
        
    }
    
}