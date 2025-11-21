// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SimHub.Plugins.PropertyServer.AutoUpdate
{
    /// <summary>
    /// Handles automatic updates by checking GitHub releases and downloading new versions.
    /// </summary>
    public class AutoUpdater
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/pre-martin/SimHubPropertyServer/releases/latest";
        private const string UserAgent = "SimHubPropertyServer-Updater";

        public async Task<GitHubVersionInfo> GetLatestVersion()
        {
            HttpResponseMessage response;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                response = await httpClient.GetAsync(GitHubApiUrl);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GitHubVersionInfo>(content);
        }

        /// <summary>
        /// Downloads and applies the latest version of the PropertyServer plugin.
        /// </summary>
        /// <exception cref="Exception">Is thrown for various I/O problems or validation errors</exception>
        public async Task Update()
        {
            var versionInfo = await GetLatestVersion();
            var asset = versionInfo.Assets.Find(a => a.Name == "PropertyServer.dll");
            if (asset == null)
            {
                throw new Exception("Asset \"PropertyServer.dll\" not found in GitHub release.");
            }

            var size = asset.Size;
            var digest = asset.Digest;
            var downloadUrl = asset.BrowserDownloadUrl;

            await DownloadAndVerifyAsync(downloadUrl, digest, size, "PropertyServer.dll.new");

            File.Delete("PropertyServer.dll.old");
            File.Move("PropertyServer.dll", "PropertyServer.dll.old");
            File.Move("PropertyServer.dll.new", "PropertyServer.dll");
        }

        private async Task DownloadAndVerifyAsync(string downloadUrl, string expectedDigest, long expectedSize, string targetFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(targetFilePath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }

            var fileInfo = new FileInfo(targetFilePath);
            if (fileInfo.Length != expectedSize)
            {
                throw new Exception($"Downloaded file size mismatch. Expected: {expectedSize}, actual: {fileInfo.Length}");
            }

            if (expectedDigest.StartsWith("sha256:"))
            {
                var sha256Digest = expectedDigest.Substring("sha256:".Length);
                using (var fileStream = File.OpenRead(targetFilePath))
                using (var sha256 = SHA256.Create())
                {
                    var computedHash = sha256.ComputeHash(fileStream);
                    var computedHashString = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(computedHashString, sha256Digest, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"SHA256 digest mismatch for downloaded file. Expected: {sha256Digest}, actual: {computedHashString}");
                    }
                }
            }
        }
    }
}