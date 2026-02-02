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
// File: UpdateChecker.cs
// ============================================================================

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

internal sealed class UpdateChecker
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _assetHint;

    public UpdateChecker(string owner, string repo, string assetHint)
    {
        _owner = owner;
        _repo = repo;
        _assetHint = assetHint;
    }

    public async Task<UpdateResult> CheckAsync()
    {
        string json;

        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string url = string.Format(
                "https://api.github.com/repos/{0}/{1}/releases/latest",
                _owner,
                _repo);

            json = await client.GetStringAsync(url).ConfigureAwait(false);
        }

        GitHubRelease release = Deserialize(json);
        if (release == null || release.Assets == null)
            return UpdateResult.Failed("Invalid GitHub response");

        Version latest = NormalizeGitHubVersion(release.TagName);
        Version current = GetCurrentVersion();

#if DEBUG
        ; // NOP out the version check in DEBUG builds so the update available label is always visible with valid links.
#else
        if (latest <= current)
            return UpdateResult.None(current, latest);
#endif

        GitHubAsset asset = release.Assets
            .FirstOrDefault(a =>
                a.Name != null &&
                a.Name.IndexOf(_assetHint, StringComparison.OrdinalIgnoreCase) >= 0);

        if (asset == null || string.IsNullOrEmpty(asset.DownloadUrl))
            return UpdateResult.Failed("Matching asset not found");

        string releasePageUrl = string.Format(
            "https://github.com/{0}/{1}/releases/tag/{2}",
            _owner,
            _repo,
            release.TagName);

        return UpdateResult.Available(
            current,
            latest,
            asset.Name,
            asset.DownloadUrl,
            releasePageUrl);
    }

    /*
     * Helper methods
     */
    private static Version GetCurrentVersion()
    {
        Version v = Assembly.GetExecutingAssembly().GetName().Version;
        return v ?? new Version(0, 0, 0, 0);
    }

    private static Version NormalizeGitHubVersion(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return new Version(0, 0, 0, 0);

        string v = tag.TrimStart('v', 'V');

        Version parsed;
        if (!Version.TryParse(v, out parsed))
            return new Version(0, 0, 0, 0);

        if (parsed.Revision < 0)
            return new Version(parsed.Major, parsed.Minor, parsed.Build, 0);

        return parsed;
    }

    private static GitHubRelease Deserialize(string json)
    {
        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            DataContractJsonSerializer ser =
                new DataContractJsonSerializer(typeof(GitHubRelease));

            return ser.ReadObject(ms) as GitHubRelease;
        }
    }


    internal sealed class UpdateResult
    {
        public bool IsUpdateAvailable { get; private set; }
        public Version Current { get; private set; }
        public Version Latest { get; private set; }
        public string AssetName { get; private set; }
        public string DownloadUrl { get; private set; }
        public string Error { get; private set; }
        public string ReleasePageUrl { get; private set; }

        private UpdateResult() { }

        public static UpdateResult None(Version current, Version latest)
        {
            return new UpdateResult
            {
                IsUpdateAvailable = false,
                Current = current,
                Latest = latest
            };
        }

        public static UpdateResult Available(
            Version current,
            Version latest,
            string assetName,
            string downloadUrl,
            string releasePageUrl)
        {
            return new UpdateResult
            {
                IsUpdateAvailable = true,
                Current = current,
                Latest = latest,
                AssetName = assetName,
                DownloadUrl = downloadUrl,
                ReleasePageUrl = releasePageUrl
            };
        }

        public static UpdateResult Failed(string message)
        {
            return new UpdateResult
            {
                IsUpdateAvailable = false,
                Error = message
            };
        }
    }


    [DataContract]
    private sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "assets")]
        public GitHubAsset[] Assets { get; set; }
    }

    [DataContract]
    private sealed class GitHubAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string DownloadUrl { get; set; }
    }
}
