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
// File: ProgressForm.cs
// ============================================================================
using System;
using System.Threading;
using System.Windows.Forms;

namespace ProScanMultiUpdater
{
    public partial class ProgressForm : Form
    {
        private CancellationTokenSource _cts;

        public ProgressForm()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int percentage, long bytesReceived, long totalBytes)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(percentage, bytesReceived, totalBytes)));
                return;
            }

            progressBar.Value = Math.Min(percentage, 100);

            if (totalBytes > 0)
            {
                string received = FormatBytes(bytesReceived);
                string total = FormatBytes(totalBytes);
                labelStatus.Text = $"Downloading... {received} / {total} ({percentage}%)";
            }
            else
            {
                labelStatus.Text = $"Downloading... {FormatBytes(bytesReceived)}";
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public void AttachCancellation(CancellationTokenSource cts)
        {
            _cts = cts;
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancel.Enabled = false;
            btnCancel.Text = "Canceling...";
            _cts?.Cancel();
        }
    }
}
