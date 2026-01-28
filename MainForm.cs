/*

MIT License

Copyright (c) 2026 Nick DeBaggis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

// ============================================================================
// File: MainForm.cs
// ============================================================================
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ProScanMultiUpdater
{
    public partial class MainForm : Form
    {
        private List<ProcessInfo> foundProcesses;
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static string asmname = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        // Get .NET runtime version
        private static string dotNetVersion = RuntimeInformation.FrameworkDescription;

        // Get OS details
        private static string osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        private static string osInfo = $"{dotNetVersion}; {GetWindowsVersion()}; {osArchitecture}";
        private static string u_agent = $"Mozilla/5.0 ({asmname}/{version} {osInfo})";

        private static readonly HttpClient sharedClient = new HttpClient
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan // we control timeouts
        };

        public MainForm()
        {
            InitializeComponent();
            this.HelpRequested += MainForm_HelpRequested; // F1 will open the github project page.
            sharedClient.DefaultRequestHeaders.UserAgent.ParseAdd(u_agent);

            string tlsinfo = TlsBootstrapper.Initialize("https://proscan.org");
            txtOutput.AppendText($"Multi-Instance Updater for ProScan, ver:{version}, (c)2026 https://github.com/pahtzo/ProScanMultiUpdater\r\n");
            txtOutput.AppendText($"OS Info: {osInfo}\r\n");
#if DEBUG
            this.Text = this.Text + " - DEBUG BUILD";
            txtOutput.AppendText($"\r\nDEBUG BUILD\r\n");
            txtOutput.AppendText($"Assembly: {asmname} version: {version}\r\n");
            txtOutput.AppendText($"UserAgent: {u_agent}\r\n");
            txtOutput.AppendText($"{tlsinfo}\r\n");
#endif
            CheckAdministrator();
            foundProcesses = new List<ProcessInfo>();
            labelProcsFound.Text = "Processes found: " + foundProcesses.Count;
        }

        private static string GetWindowsVersion()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            if (key == null)
                return "Unknown Windows";

            string productName = key.GetValue("ProductName") as string ?? "Windows";
            string buildStr = key.GetValue("CurrentBuild") as string ?? "0";
            string displayVersion =
                key.GetValue("DisplayVersion") as string ??
                key.GetValue("ReleaseId") as string;

            int.TryParse(buildStr, out int build);

            // Windows 11 detection
            if (build >= 22000)
            {
                productName = productName.Replace("Windows 10", "Windows 11");
            }

            return displayVersion != null
                ? $"{productName} {displayVersion} (Build {build})"
                : $"{productName} (Build {build})";
        }

        /*
         * Check if running elevated.  Notify otherwise and exit.
         * Shouldn't fire as long as app.manifest contains:
         * <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
         * in the requestedPrivileges key.
         */
        private void CheckAdministrator()
        {
            bool isAdmin = false;

            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                if (isAdmin)
                {
                    txtOutput.AppendText($"\r\nRunning as: {identity.Name} Elevated: {isAdmin}\r\n");
                }
                identity.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking administrator status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!isAdmin)
            {
                MessageBox.Show("This application must be run as Administrator.\r\n\r\n" +
                    "Please right-click the executable and select 'Run as administrator'.",
                    "Administrator Rights Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Environment.Exit(1);
            }
        }


        private void PopulateProcessGrid()
        {
            try
            {
                dataGridView1.SuspendLayout();
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
                checkboxColumn.HeaderText = "Update";
                checkboxColumn.Name = "Update";
                checkboxColumn.Width = 60;
                checkboxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns.Add(checkboxColumn);

                dataGridView1.Columns.Add("ProcessName", "Process Name");
                dataGridView1.Columns.Add("ProcessId", "Process ID");
                dataGridView1.Columns.Add("Path", "Path");
                dataGridView1.Columns.Add("MainWindowTitle", "Window Title");
                dataGridView1.Columns.Add("ProductVersion", "Version");
                dataGridView1.Columns.Add("StartTime", "Start Time");
                dataGridView1.Columns.Add("RunningAs", "Running-As");

                foreach (var procInfo in foundProcesses)
                {
                    try
                    {
                        dataGridView1.Rows.Add(
                            false,
                            procInfo.Process.ProcessName,
                            procInfo.Id,
                            procInfo.Path ?? "N/A",
                            procInfo.MainWindowTitle ?? "",
                            procInfo.ProductVersion ?? "N/A",
                            procInfo.StartTime != DateTime.MinValue ? procInfo.StartTime.ToString("g") : "N/A",
                            procInfo.UserName ?? "N/A"
                        );
                    }
                    catch (Exception ex)
                    {
                        txtOutput.AppendText($"Error adding process to grid: {ex.Message}\r\n\r\n");
                    }
                }

                // make the grid read-only except the Update column checkboxes.
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    if (column.Name != "Update")
                    {
                        column.ReadOnly = true;
                    }
                }

                // auto-resize columns to longest string in each column.
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Sort by Path column (ascending)
                if (dataGridView1.Columns.Contains("Path"))
                {
                    dataGridView1.Sort(dataGridView1.Columns["Path"], System.ComponentModel.ListSortDirection.Ascending);
                }
                dataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Error populating grid: {ex.Message}\r\n\r\n");
            }
            finally
            {
                dataGridView1.ResumeLayout();
            }
        }

        /*
         * Collect the processes the user selected in the grid for update.
         */
        private List<ProcessInfo> GetSelectedProcesses()
        {
            var selectedProcesses = new List<ProcessInfo>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Update"].Value != null &&
                    (bool)row.Cells["Update"].Value == true)
                {
                    int processId = int.Parse(row.Cells["ProcessId"].Value?.ToString() ?? "0");
                    var procInfo = foundProcesses.FirstOrDefault(p => p.Id == processId);
                    if (procInfo != null)
                    {
                        selectedProcesses.Add(procInfo);
                    }
                }
            }
            return selectedProcesses;
        }


        private void SetAllCheckboxes(bool isChecked)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["Update"].Value = isChecked;
            }
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            foundProcesses.Clear();

            txtOutput.AppendText("\r\n" + new string('=', 85) + "\r\n");
            txtOutput.AppendText($"PROCESS/INSTANCE SCAN STARTED AT {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\r\n");
            txtOutput.AppendText(new string('=', 85) + "\r\n\r\n");

            try
            {
                Process[] procs = Process.GetProcessesByName("ProScan");

                if (procs.Length == 0)
                {
                    txtOutput.AppendText("No ProScan processes running!\r\n" +
                        "The updater will only update instances that are found running on the system\r\n" +
                        "Launch all of your ProScan instances then click \"Scan For Running ProScan Instances\"\r\n");

                    btnKillAndUpdate.Enabled = false;
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    MessageBox.Show("No ProScan processes running!\n\n" +
                        "The updater will only update instances that are found running on the system\n\n" +
                        "Launch all of your ProScan instances then click \"Scan For Running ProScan Instances\"",
                        "No Processes Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                        );

                    return;
                }
                // optimize the process info log output strings to a single batch inside the for loop.
                StringBuilder logoutput = new StringBuilder();
                
                foreach (Process proc in procs)
                {
                    // Skip processes that don't match the copyright prefix & suffix, not anywhere near fool-proof but it'll have to do.
                    // A better option would be checking the digital signature of the exe if it was code-signed.
                    if (!CheckCopyrightMatches(proc, "ProScan", "Bob Aune")) { continue; };

                    try
                    {
                        ProcessInfo info = new ProcessInfo
                        {
                            Process = proc,
                            Id = proc.Id,
                            Path = GetProcessPath(proc),
                            MainWindowTitle = proc.MainWindowTitle,
                            ProductVersion = GetProductVersion(proc),
                            StartTime = GetStartTime(proc)
                        };

                        // Capture token information for relaunching
                        ProcessRelauncher.CaptureTokenInfo(info);

                        foundProcesses.Add(info);

                        logoutput.Append($"Process window: {info.MainWindowTitle}\r\n");
                        logoutput.Append($"Process path: {info.Path}\r\n");
                        logoutput.Append($"Process ID: {info.Id}\r\n");
                        logoutput.Append($"Product version: {info.ProductVersion}\r\n");
                        logoutput.Append($"Process started: {info.StartTime}\r\n");
 
                        if (!string.IsNullOrEmpty(info.UserName))
                        {
                            logoutput.Append($"Running as user: {info.UserName}\r\n");
                        }
                        logoutput.Append(new string('=', 85) + "\r\n");
                    }
                    catch (Exception ex)
                    {
                        logoutput.Append($"Error processing PID {proc.Id}: {ex.Message}\r\n\r\n");
                    }
                }
                txtOutput.AppendText(logoutput.ToString());

                PopulateProcessGrid();
                btnKillAndUpdate.Enabled = foundProcesses.Count > 0;
                labelProcsFound.Text = "Processes found: " + foundProcesses.Count;
                txtOutput.AppendText($"Processes found: {foundProcesses.Count}\r\n\r\n");
                
                if(foundProcesses.Count == 0)
                {
                    txtOutput.AppendText("No ProScan processes running!\r\n" +
                        "The updater will only update instances that are found running on the system\r\n" +
                        "Launch all of your ProScan instances then click \"Scan For Running ProScan Instances\"\r\n");

                    btnKillAndUpdate.Enabled = false;
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    MessageBox.Show("No ProScan processes running!\n\n" +
                        "The updater will only update instances that are found running on the system\n\n" +
                        "Launch all of your ProScan instances then click \"Scan For Running ProScan Instances\"",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                        );

                    return;
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Error scanning processes: {ex.Message}\r\n\r\n");
                MessageBox.Show($"Error scanning processes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task DownloadProscan()
        {
            // ProScan official website
            string websiteUrl = "https://www.proscan.org";

            /*
             * The user selected to download the latest version so we're going to save and run that out of
             * the TEMP path.  A new sub-directory will be created under TEMP.
             * The sub-directory and extracted EXE setup installer will remain after we're done,
             * however the original downloaded ZIP file will be removed.
             */
            string downloadPath = Path.GetTempPath();

            try
            {
                txtOutput.AppendText("\r\n" + new string('=', 85) + "\r\n");
                txtOutput.AppendText("CHECKING WEBSITE FOR LATEST INSTALLER ZIP FILE\r\n");
                txtOutput.AppendText(new string('=', 85) + "\r\n");

                /*
                 * if FindZipFileAsync fails to connect, times out, or can't find the zip file
                 * we'll return to the caller and prompt to download and extract manually and use that instead.
                 */
                string zipFileUrl = await FindZipFileAsync(websiteUrl);

                if (string.IsNullOrEmpty(zipFileUrl))
                {
                    txtOutput.AppendText("Error: No zip file found on the website.\r\n");
                    return;
                }

                string zipFileName = Path.GetFileName(new Uri(zipFileUrl).LocalPath);
                txtOutput.AppendText($"Found: {zipFileUrl}\r\n");

                /*
                 * Add MessageBoxDefaultButton.Button2 at the end to make "No" the default option.
                 */
                DialogResult response =
                    MessageBox.Show(
                        $"Do you want to download and update using the latest version: {zipFileName}?",
                        "Download",
                        MessageBoxButtons.YesNo);

                if (response != DialogResult.Yes)
                {
                    txtOutput.AppendText($"Download canceled by user.\r\n");
                    return;
                }

                txtOutput.AppendText($"Download latest version started at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\r\n");

                string downloadedFilePath = Path.Combine(downloadPath, zipFileName);
                txtOutput.AppendText($"\nDownloading to: {downloadedFilePath}\r\n");
//                
                if(await DownloadFileAsync(zipFileUrl, downloadedFilePath) != true)
                {
                    txtOutput.AppendText($"Download canceled by user.\r\n");
                    return;
                }

                txtOutput.AppendText("Unblocking file...\r\n");
                UnblockFile(downloadedFilePath);

                string extractPath = Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(zipFileName));
                txtOutput.AppendText($"\nExtracting to: {extractPath}\r\n");
                ExtractZipFile(downloadedFilePath, extractPath);

                string exeFileName = Path.GetFileNameWithoutExtension(zipFileName) + ".exe";
                string exePath = FindExeFile(extractPath, exeFileName);

                if (!string.IsNullOrEmpty(exePath))
                {
                    txtSetupPath.Text = exePath;
                    txtOutput.AppendText($"\nFound installer: {exePath}, changing to the downloaded version.\r\n");
                }
                else
                {
                    txtOutput.AppendText($"\nError: {exeFileName} not found in the extracted files.\r\n");
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"\nError: {ex.Message}\r\n");
            }
        }
        /*
         * Site scraper to find the ProScan_XX_YY.zip installer archive file on the main
         * ProScan website page.
         */
        private async Task<string> FindZipFileAsync(string url)
        {
            using (var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(30)))
            {
                try
                {
                    using (HttpResponseMessage response =
                        await sharedClient.GetAsync(url, timeoutCts.Token))
                    {
                        response.EnsureSuccessStatusCode();

                        string html = await response.Content.ReadAsStringAsync();

                        var zipMatch = Regex.Match(
                            html,
                            @"(?:href|src)=[""']([^""']*ProScan_\d+_\d+\.zip)[""']",
                            RegexOptions.IgnoreCase);

                        if (!zipMatch.Success)
                        {
                            zipMatch = Regex.Match(
                                html,
                                @"(https?://[^\s<>""']*ProScan_\d+_\d+\.zip)",
                                RegexOptions.IgnoreCase);
                        }

                        if (!zipMatch.Success)
                            return null;

                        string zipPath = zipMatch.Groups[1].Value;

                        if (!zipPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            Uri baseUri = new Uri(url);
                            zipPath = new Uri(baseUri, zipPath).ToString();
                        }

                        return zipPath;
                    }
                }
                catch (OperationCanceledException)
                {
                    txtOutput.AppendText(
                        $"Request timed out checking {url} for installer.\r\n\r\n");
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    txtOutput.AppendText(
                        $"HTTP error checking {url} for installer: {ex.Message}\r\n\r\n");
                    return null;
                }
            }
        }

        /*
         * Async file downloader.
         */
        private async Task<bool> DownloadFileAsync(string url, string destinationPath)
        {
            string tempPath = destinationPath + ".part";
            bool success = false;
            bool wasCanceled = false;

            var cts = new CancellationTokenSource();
            var progressForm = new ProgressForm();
            progressForm.AttachCancellation(cts);
            progressForm.Show();

            try
            {
                using (HttpResponseMessage response =
                    await sharedClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? -1;
                    long receivedBytes = 0;

                    using (var input = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[81920];
                        int bytesRead;

                        while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, bytesRead, cts.Token);
                            receivedBytes += bytesRead;

                            int percent = totalBytes > 0
                                ? (int)(receivedBytes * 100 / totalBytes)
                                : 0;

                            progressForm.UpdateProgress(percent, receivedBytes, totalBytes);
                        }
                    }
                }

                // Only reached if everything completed normally
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Move(tempPath, destinationPath);
                success = true;
            }
            catch (OperationCanceledException)
            {
                wasCanceled = true;
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Download error: {ex}");
            }
            finally
            {
                progressForm.Close();
                progressForm.Dispose();

                if (!success && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }

            return success && !wasCanceled;
        }

        /*
         * Windows MoTW flag remover.  Unblocks a file downloaded from the Internet by
         * removing the Zone.Identifier alternate data stream from the file.
         */
        private static void UnblockFile(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string zoneIdentifier = filePath + ":Zone.Identifier";
                if (File.Exists(zoneIdentifier))
                {
                    File.Delete(zoneIdentifier);
                }
            }
        }

        /*
         * .NET Framework 4.7.2.  ExtractToDirectory will throw an exception if the destination directory
         * contains the same named files as in the archive being extracted.
         * 
         * .NET Core 2.0 and later have a boolean parameter to allow overwriting existing files.
         * 
         * For now we'll stick with the earlier implementation to match ProScan's OS compatibility of
         * Windows 7 SP1 and .NET Framework 4.7.2 and newer.
         */
        private void ExtractZipFile(string zipPath, string extractPath)
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);
        }


        private async void BtnKillAndUpdate_Click(object sender, EventArgs e)
        {
            var processesToUpdate = GetSelectedProcesses();

            if (processesToUpdate.Count == 0)
            {
                MessageBox.Show("No processes selected for update.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await DownloadProscan();

            if (string.IsNullOrWhiteSpace(txtSetupPath.Text))
            {
                MessageBox.Show("Please browse for the ProScan setup installer or confirm to download the latest version when prompted.", "Setup Path Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtSetupPath.Text))
            {
                MessageBox.Show("The specified setup file does not exist.", "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            /*
             * Inno Setup basic fingerprint checking
             * Checks if the installer assembly has a manifest and the name component contains the typical Inno Setup string JR.Inno.Setup,
             * we also check if the file version info contains the ProductName: ProScan
             * 
             * This will check both a user-supplied installer as well as the installer we download from the proscan site if the user
             * confirmed that option.  It's a basic check and digital signature checks would be more robust.
             */

            if (!ProScanSetupFingerprint.IsExpectedInstaller(txtSetupPath.Text))
            {
                // NO GOOD
                txtOutput.AppendText($"The specified setup file {txtSetupPath.Text} is not a ProScan Setup Installer!\r\n\r\n");
                MessageBox.Show($"The specified setup file {txtSetupPath.Text} is not a ProScan Setup Installer!", "Wrong Installer",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"WARNING! if you have Favorite or Profile editors open please save your work before continuing.\r\n\r\n" +
                $"This will stop {processesToUpdate.Count} selected process(es) and run the installer for each install directory.\r\n\r\nContinue?",
                "Confirm Action",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
                );

            if (result != DialogResult.Yes)
            {
                txtOutput.AppendText("Install canceled by user.\r\n");
                return;
            }

            txtOutput.AppendText("\r\n" + new string('=', 85) + "\r\n");
            txtOutput.AppendText($"SWITCHING VIEW TO LOGGING TAB\r\n");
            txtOutput.AppendText(new string('=', 85) + "\r\n");

            tabControl1.SelectedTab = tabLogging;

            txtOutput.AppendText($"Update instances started at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\r\n");
            txtOutput.AppendText($"Using setup installer: {txtSetupPath.Text}\r\n");
            txtOutput.AppendText($"Count of processes selected for update: {processesToUpdate.Count}\r\n");

            txtOutput.AppendText("\r\n" + new string('=', 85) + "\r\n");
            txtOutput.AppendText("STOPPING PROCESSES AND RUNNING INSTALLER\r\n");
            txtOutput.AppendText(new string('=', 85) + "\r\n\r\n");

            var installDirs = processesToUpdate
                .Where(p => !string.IsNullOrEmpty(p.Path) && File.Exists(p.Path))
                .Select(p => Path.GetDirectoryName(p.Path))
                .Distinct()
                .ToList();

            foreach (var procInfo in processesToUpdate)
            {
                if (!IsProcessRunning(procInfo.Id))
                {
                    txtOutput.AppendText($"Process already terminated, skipping close/kill on: {procInfo.ExecutablePath} PID: {procInfo.Id}\r\n");
                    txtOutput.AppendText($"Continuing the update for the directory: {Path.GetDirectoryName(procInfo.ExecutablePath)}\r\n\r\n");
                }
                else
                {
                    try
                    {
                        txtOutput.AppendText($"Terminating process: {procInfo.ExecutablePath} PID: {procInfo.Id}\r\n");
                        txtOutput.AppendText($"Attempting graceful shutdown via CloseMainWindow...\r\n");

                        if (!await CloseAndWaitAsync(procInfo.Process, 10000))
                        {
                            txtOutput.AppendText($"Graceful shutdown failed or timed out, {procInfo.ExecutablePath} will be killed\r\n");
                            procInfo.Process.Kill();
                            procInfo.Process.WaitForExit();
                        }

                        txtOutput.AppendText($"Process {procInfo.ExecutablePath} PID: {procInfo.Id} terminated successfully.\r\n\r\n");
                    }
                    catch (Exception ex)
                    {
                        txtOutput.AppendText($"Error terminating process {procInfo.ExecutablePath} PID: {procInfo.Id}: {ex.Message}\r\n\r\n");
                    }
                }
            }

            txtOutput.AppendText(new string('-', 85) + "\r\n");
            txtOutput.AppendText("RUNNING INSTALLER FOR EACH INSTALL DIRECTORY\r\n");
            txtOutput.AppendText(new string('-', 85) + "\r\n");

            foreach (var installDir in installDirs)
            {
                try
                {
                    RunProscanInstaller(installDir);
                }
                catch (Exception ex)
                {
                    txtOutput.AppendText($"Error running installer for {installDir}: {ex.Message}\r\n\r\n");
                }
            }

            if (checkBoxRestartProcs.Checked == true)
            {
                txtOutput.AppendText("\r\n" + new string('-', 85) + "\r\n");
                txtOutput.AppendText("RESTARTING PROCESSES AS ORIGINAL USER\r\n");
                txtOutput.AppendText(new string('-', 85) + "\r\n");

                foreach (var procInfo in processesToUpdate)
                {
                    try
                    {
                        txtOutput.AppendText($"Attempting to start {procInfo.Path} at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                        
                        if (!string.IsNullOrEmpty(procInfo.UserName))
                        {
                            txtOutput.AppendText($" as user: {procInfo.UserName}\r\n");
                        }
                        else
                        {
                            txtOutput.AppendText($"\r\n");
                        }

                        bool success = ProcessRelauncher.RelaunchAsOriginalUser(
                                procInfo.ExecutablePath ?? procInfo.Path,
                                procInfo.Arguments,
                                procInfo.WorkingDirectory,
                                procInfo.UserToken);

                        if (success)
                        {
                            txtOutput.AppendText($"Process restarted successfully.\r\n\r\n");
                        }
                        else
                        {
                            txtOutput.AppendText($"Error restarting process {procInfo.ExecutablePath}\r\n\r\n");
                        }

                        // Clean up token
                        procInfo.Dispose();
                    }
                    catch (Exception ex)
                    {
                        txtOutput.AppendText($"Error starting process {procInfo.Path}: {ex.Message}\r\n\r\n");
                    }
                }
            }
            else
            {
                // user didn't select to restart the process instances.
                txtOutput.AppendText($"You may now start your ProScan instances.\r\n\r\n");
                
                // Clean up tokens even if not restarting
                foreach (var procInfo in processesToUpdate)
                {
                    procInfo.Dispose();
                }
            }
            txtOutput.AppendText($"Update completed {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\r\n");
            txtOutput.AppendText($"\r\nTo save this log right-click and Save-As\r\n");

            foundProcesses.Clear();
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            btnKillAndUpdate.Enabled = false;
            labelProcsFound.Text = "Processes found: " + foundProcesses.Count;
        }

        
        private static bool IsProcessRunning(int pid)
        {
            try
            {
                Process proc = Process.GetProcessById(pid);
                return !proc.HasExited;
            }
            catch (ArgumentException)
            {
                // Thrown if process with PID does not exist
                return false;
            }
        }

        
        private static async Task<bool> CloseAndWaitAsync(Process process, int timeoutMs)
        {
            if (process.HasExited)
                return true;

            process.CloseMainWindow();

            return await Task.Run(() =>
                process.WaitForExit(timeoutMs)
            ).ConfigureAwait(false);
        }

        
        private void RunProscanInstaller(string installDir)
        {
            txtOutput.AppendText($"Install directory: {installDir}\r\n");

            string logPath = Path.Combine(installDir, "ProScanMultiUpdater-install.log");

            /*
             * This shouldn't happen but if we weren't able to close/kill the ProScan processes ourselves then inform the
             * ProScan Inno Setup installer to force close them with /FORCECLOSEAPPLICATIONS.  This would only affect
             * EXE's/DLL's that are independent from ProScan.exe and still loaded/running, i.e. * "Profile Editor.exe",
             * "RemoveActivation.exe", or the uninstaller stub.
             * 
             * The user was informed, prior to continuing, to save any work if they had Favorite/Profile editor changes pending.
             */

            string arguments =
                $"/VERYSILENT /LOG=\"{logPath}\" /DIR=\"{installDir}\" " +
                 "/MERGETASKS=\"!desktopicon\" /NOICONS " +
                 "/NORESTART /FORCECLOSEAPPLICATIONS";

            txtOutput.AppendText($"Running: \"{txtSetupPath.Text}\" {arguments}\r\n");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = txtSetupPath.Text,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    txtOutput.AppendText($"Installer started (PID: {process.Id}), waiting for completion...\r\n");
                    process.WaitForExit();
                    txtOutput.AppendText($"Installer completed with exit code: {process.ExitCode}\r\n");
                    txtOutput.AppendText($"Install log: {logPath}\r\n\r\n");
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Error starting installer: {ex.Message}\r\n\r\n");
            }
        }


        private void SaveLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Log File";
                saveFileDialog.FileName = $"ProScanMultiUpdater-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, txtOutput.Text);
                        MessageBox.Show($"Log saved successfully to:\r\n{saveFileDialog.FileName}",
                            "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving log file: {ex.Message}",
                            "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string GetProcessPath(Process proc)
        {
            try
            {
                return proc.MainModule?.FileName;
            }
            catch
            {
                return "Access denied";
            }
        }

        /*
         * Check the copyright string of a running executable.
         */
        private static bool CheckCopyrightMatches(Process proc, string requiredPrefix, string requiredSuffix)
        {
            string proc_copyright = proc.MainModule?.FileVersionInfo.LegalCopyright;

            if (string.IsNullOrWhiteSpace(proc_copyright))
                return false;

            string text = Normalize(proc_copyright);

            // Remove any 4-digit year (1900–2099)
            text = Regex.Replace(text, @"\b(19|20)\d{2}\b", "");

            return text.StartsWith(Normalize(requiredPrefix)) && text.EndsWith(Normalize(requiredSuffix));
        }

        /*
         * Convert a string to lower-case and trim leading and trailing space.
         */
        private static string Normalize(string s)
        {
            return s
                .ToLowerInvariant()
                .Trim();
        }

        private string GetProductVersion(Process proc)
        {
            try
            {
                return proc.MainModule?.FileVersionInfo.ProductVersion;
            }
            catch
            {
                return "N/A";
            }
        }

        private DateTime GetStartTime(Process proc)
        {
            try
            {
                return proc.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string FindExeFile(string directoryPath, string exeFileName)
        {
            string[] exeFiles = Directory.GetFiles(directoryPath, exeFileName, SearchOption.AllDirectories);
            if (exeFiles.Length > 0)
            {
                return exeFiles[0];
            }
            return null;
        }


        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable files (ProScan_*_*.exe)|ProScan_*_*.exe";
                openFileDialog.Title = "Select ProScan Installer File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtSetupPath.Text = openFileDialog.FileName;
                    UnblockFile(txtSetupPath.Text);
                }
            }
        }


        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            SetAllCheckboxes(true);
        }


        private void BtnDeselectAll_Click(object sender, EventArgs e)
        {
            SetAllCheckboxes(false);
        }


        /*
         * When the main form is loaded and presented run an initial process scan and pre-fill
         * the found procs count label.
         */
        private void MainForm_Shown(object sender, EventArgs e)
        {
            BtnScan_Click(sender, e);
            labelProcsFound.Text = "Processes found: " + foundProcesses.Count;
        }

        /*
         * Tail the txtOutput form tab control when the tab has focus.
         */
        private void txtOutput_TextChanged(object sender, EventArgs e)
        {
            txtOutput.SelectionStart = txtOutput.Text.Length;
            txtOutput.ScrollToCaret();
        }

        private void MainForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            OpenHelpUrl();
            hlpevent.Handled = true; // prevents default Windows help behavior
        }

        private void OpenHelpUrl()
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/pahtzo/ProScanMultiUpdater/tree/main?tab=readme-ov-file#readme",
                UseShellExecute = true
            });

        }
        
    }
}
