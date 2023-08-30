﻿//----------------------------------------------------------------------------------------------
//    Copyright 2023 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//---------------------------------------------------------------------------------------------


using Newtonsoft.Json;

namespace MK.IO.Models
{
    public class MKIOAssetProperties
    {
        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("lastModified")]
        public string LastModified { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("container")]
        public string Container { get; set; }

        [JsonProperty("storageAccountName")]
        public string StorageAccountName { get; set; }

        [JsonProperty("storageEncryptionFormat")]
        public string StorageEncryptionFormat { get; set; }

        [JsonProperty("encryptionScope")]
        public string EncryptionScope { get; set; }
    }
}