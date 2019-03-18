﻿// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Graph.Auth
{
    using System;
    using Newtonsoft.Json;

    internal partial class JwtPayload
    {
        [JsonProperty("aud")]
        public Uri Aud { get; set; }

        [JsonProperty("iss")]
        public Uri Iss { get; set; }

        [JsonProperty("app_displayname")]
        public string AppDisplayname { get; set; }

        [JsonProperty("appid")]
        public Guid Appid { get; set; }

        [JsonProperty("deviceid")]
        public Guid Deviceid { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        [JsonProperty("ipaddr")]
        public string Ipaddr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("oid")]
        public Guid Oid { get; set; }

        [JsonProperty("scp")]
        public string Scp { get; set; }

        [JsonProperty("tid")]
        public Guid Tid { get; set; }

        [JsonProperty("unique_name")]
        public string UniqueName { get; set; }

        [JsonProperty("upn")]
        public string Upn { get; set; }
    }
}
