﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MK.IO.Models
{
    /// <summary>
    /// Specifies a symmetric key for token validation.
    /// </summary>
    public class ContentKeyPolicySymmetricTokenKey : ContentKeyPolicyVerificationKey
    {
        public ContentKeyPolicySymmetricTokenKey(string keyValue)
        {
            KeyValue = keyValue;
        }

        [JsonProperty("@odata.type")]
        internal override string OdataType => "#Microsoft.Media.ContentKeyPolicySymmetricTokenKey";

        /// <summary>
        /// The key value of the key
        /// </summary>
        /// <value>The key value of the key</value>
        [JsonProperty("keyValue")]
        public string KeyValue { get; set; }
    }
}
