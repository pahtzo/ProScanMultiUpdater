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
// File: ProScanSetupFingerprint.cs
// ============================================================================

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace ProScanMultiUpdater
{
    internal class ProScanSetupFingerprint
    {
        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        private const int RT_MANIFEST = 24;

        // it's an Inno Setup installer.
        private const string ExpectedManifestName = "JR.Inno.Setup";

        // EXE product name is ProScan.
        private const string ExpectedProductName = "ProScan";

        public static bool IsExpectedInstaller(string exePath)
        {
            if (!HasExpectedManifestName(exePath))
                return false;

            return HasExpectedProductName(exePath);
        }

        private static bool HasExpectedProductName(string exePath)
        {
            try
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exePath);

                if (string.IsNullOrEmpty(vi.ProductName))
                    return false;

                return string.Equals(
                    vi.ProductName.Trim(),
                    ExpectedProductName,
                    StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static bool HasExpectedManifestName(string exePath)
        {
            IntPtr hModule = LoadLibraryEx(exePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (hModule == IntPtr.Zero)
                return false;

            try
            {
                IntPtr hRes = FindResource(hModule, (IntPtr)1, (IntPtr)RT_MANIFEST);
                if (hRes == IntPtr.Zero)
                    return false;

                IntPtr hData = LoadResource(hModule, hRes);
                IntPtr pData = LockResource(hData);
                int size = SizeofResource(hModule, hRes);

                byte[] bytes = new byte[size];
                Marshal.Copy(pData, bytes, 0, size);

                string xml = Encoding.UTF8.GetString(bytes);

                XDocument doc = XDocument.Parse(xml);
                XNamespace ns = "urn:schemas-microsoft-com:asm.v1";

                XElement asmId = doc.Root.Element(ns + "assemblyIdentity");
                if (asmId == null)
                    return false;

                XAttribute nameAttr = asmId.Attribute("name");
                if (nameAttr == null)
                    return false;

                return string.Equals(
                    nameAttr.Value,
                    ExpectedManifestName,
                    StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }

        #region Win32

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(
            string lpFileName,
            IntPtr hFile,
            int dwFlags);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr FindResource(
            IntPtr hModule,
            IntPtr lpName,
            IntPtr lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(
            IntPtr hModule,
            IntPtr hResInfo);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int SizeofResource(
            IntPtr hModule,
            IntPtr hResInfo);

        #endregion
    }
}
