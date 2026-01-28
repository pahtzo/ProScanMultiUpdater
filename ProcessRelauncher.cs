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
// File: ProcessRelauncher.cs
// ============================================================================
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ProScanMultiUpdater
{
    public class ProcessRelauncher
    {
        /// <summary>
        /// Relaunches a process as its original user
        /// If updater is elevated and target was running as a different user, uses token impersonation
        /// </summary>
        public static bool RelaunchAsOriginalUser(string executablePath, string arguments = "",
            string workingDirectory = "", IntPtr originalUserToken = default)
        {
            try
            {
                bool isUpdaterElevated = IsCurrentProcessElevated();

                if (isUpdaterElevated && originalUserToken != IntPtr.Zero && originalUserToken != default(IntPtr))
                {
                    // Updater is elevated and we have the original user's token
                    return LaunchProcessWithToken(executablePath, arguments, workingDirectory, originalUserToken);
                }
                else if (isUpdaterElevated)
                {
                    // Updater is elevated but we don't have a token
                    return LaunchProcessAsLoggedInUser(executablePath, arguments, workingDirectory);
                }
                else
                {
                    // Updater is not elevated - launch normally
                    return LaunchProcessNormally(executablePath, arguments, workingDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error relaunching process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Captures token information from a running process before killing it
        /// </summary>
        public static void CaptureTokenInfo(ProcessInfo info)
        {
            try
            {
                // Get executable path - use QueryFullProcessImageName for cross-bitness compatibility
                info.ExecutablePath = GetProcessExecutablePath(info.Process);

                if (string.IsNullOrEmpty(info.ExecutablePath))
                {
                    // Fallback to existing Path if available
                    if (!string.IsNullOrEmpty(info.Path))
                    {
                        info.ExecutablePath = info.Path;
                    }
                    else
                    {
                        try
                        {
                            info.ExecutablePath = info.Process.MainModule.FileName;
                        }
                        catch { }
                    }
                }

                // Get working directory
                try
                {
                    if (!string.IsNullOrEmpty(info.ExecutablePath))
                    {
                        info.WorkingDirectory = System.IO.Path.GetDirectoryName(info.ExecutablePath);
                    }
                }
                catch
                {
                    info.WorkingDirectory = "";
                }

                // Try to get command line arguments
                info.Arguments = GetProcessCommandLine(info.Process);

                // If updater is elevated, capture the user token
                if (IsCurrentProcessElevated())
                {
                    info.UserToken = GetProcessUserToken(info.Process);
                    info.UserName = GetProcessOwnerViaPInvoke(info.Process);
                }
                else
                {
                    info.UserToken = IntPtr.Zero;
                    info.UserName = WindowsIdentity.GetCurrent().Name;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing token info: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method to clean up a user token
        /// </summary>
        public static void CleanupToken(IntPtr token)
        {
            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
        }

        private static string GetProcessExecutablePath(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);

                if (processHandle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                int capacity = 2048;
                System.Text.StringBuilder builder = new System.Text.StringBuilder(capacity);

                if (QueryFullProcessImageName(processHandle, 0, builder, ref capacity))
                {
                    return builder.ToString();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        private static IntPtr GetProcessUserToken(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenHandle = IntPtr.Zero;
            IntPtr duplicateTokenHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(ProcessAccessFlags.QueryInformation, false, process.Id);

                if (processHandle == IntPtr.Zero)
                {
                    processHandle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);
                }

                if (processHandle == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                if (!OpenProcessToken(processHandle, TOKEN_QUERY | TOKEN_DUPLICATE, out tokenHandle))
                {
                    return IntPtr.Zero;
                }

                if (!DuplicateTokenEx(tokenHandle, TOKEN_ALL_ACCESS, IntPtr.Zero,
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary, out duplicateTokenHandle))
                {
                    return IntPtr.Zero;
                }

                return duplicateTokenHandle;
            }
            catch
            {
                if (duplicateTokenHandle != IntPtr.Zero)
                {
                    CloseHandle(duplicateTokenHandle);
                }
                return IntPtr.Zero;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        private static bool LaunchProcessWithToken(string executablePath, string arguments,
            string workingDirectory, IntPtr userToken)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = "";

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            try
            {
                string commandLine = string.IsNullOrEmpty(arguments)
                    ? $"\"{executablePath}\""
                    : $"\"{executablePath}\" {arguments}";

                bool result = CreateProcessWithTokenW(
                    userToken,
                    0,
                    null,
                    commandLine,
                    0,
                    IntPtr.Zero,
                    string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory,
                    ref si,
                    out pi);

                if (result)
                {
                    CloseHandle(pi.hProcess);
                    CloseHandle(pi.hThread);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool LaunchProcessAsLoggedInUser(string executablePath, string arguments,
            string workingDirectory)
        {
            IntPtr userToken = IntPtr.Zero;

            try
            {
                Process[] explorerProcesses = Process.GetProcessesByName("explorer");

                if (explorerProcesses.Length == 0)
                {
                    return LaunchProcessNormally(executablePath, arguments, workingDirectory);
                }

                userToken = GetProcessUserToken(explorerProcesses[0]);

                if (userToken == IntPtr.Zero)
                {
                    return LaunchProcessNormally(executablePath, arguments, workingDirectory);
                }

                return LaunchProcessWithToken(executablePath, arguments, workingDirectory, userToken);
            }
            finally
            {
                if (userToken != IntPtr.Zero)
                {
                    CloseHandle(userToken);
                }
            }
        }

        private static bool LaunchProcessNormally(string executablePath, string arguments,
            string workingDirectory)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory)
                        ? System.IO.Path.GetDirectoryName(executablePath)
                        : workingDirectory
                };

                Process process = Process.Start(startInfo);
                return process != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCurrentProcessElevated()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetProcessCommandLine(Process process)
        {
            try
            {
                if (!string.IsNullOrEmpty(process.StartInfo.Arguments))
                {
                    return process.StartInfo.Arguments;
                }
            }
            catch { }

            return "";
        }

        private static string GetProcessOwnerViaPInvoke(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(ProcessAccessFlags.QueryInformation, false, process.Id);

                if (processHandle == IntPtr.Zero)
                {
                    processHandle = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, process.Id);

                    if (processHandle == IntPtr.Zero)
                    {
                        return string.Empty;
                    }
                }

                if (!OpenProcessToken(processHandle, TOKEN_QUERY, out tokenHandle))
                {
                    return string.Empty;
                }

                using (WindowsIdentity identity = new WindowsIdentity(tokenHandle))
                {
                    return identity.Name;
                }
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        #region P/Invoke Declarations

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            int dwFlags,
            System.Text.StringBuilder lpExeName,
            ref int lpdwSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            int dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const uint TOKEN_IMPERSONATE = 0x0004;
        private const uint TOKEN_ALL_ACCESS = 0xF01FF;

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryInformation = 0x0400,
            QueryLimitedInformation = 0x1000
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        #endregion
    }
}
