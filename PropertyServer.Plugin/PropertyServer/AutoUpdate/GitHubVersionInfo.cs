// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SimHub.Plugins.PropertyServer.AutoUpdate
{
    public class GitHubVersionInfo
    {
        [JsonProperty("tag_name", Required = Required.Always)]
        public string RawTagName { get; set; }

        [JsonIgnore]
        public string TagName => RawTagName.TrimStart('v', 'V');

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("digest", Required =  Required.Always)]
        public string Digest { get; set; }

        [JsonProperty("browser_download_url", Required = Required.Always)]
        public string BrowserDownloadUrl { get; set; }
    }
}
