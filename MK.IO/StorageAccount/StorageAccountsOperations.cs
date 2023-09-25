﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using MK.IO.Models;
using Newtonsoft.Json;

namespace MK.IO
{
    /// <summary>
    /// REST Client for MKIO
    /// https://io.mediakind.com
    /// 
    /// </summary>
    public class StorageAccountsOperations : IStorageAccountsOperations
    {
        //
        // storage
        //
        private const string storageApiUrl = "api/accounts/{0}/subscriptions/{1}/storage/";
        private const string storageSelectionApiUrl = storageApiUrl + "{2}/";
        private const string storageListCredentialsApiUrl = storageSelectionApiUrl + "credentials/";
        private const string storageCredentialApiUrl = storageListCredentialsApiUrl + "{3}/";

        /// <summary>
        /// Gets a reference to the AzureMediaServicesClient
        /// </summary>
        private MKIOClient Client { get; set; }

        /// <summary>
        /// Initializes a new instance of the StorageAccountsOperations class.
        /// </summary>
        /// <param name='client'>
        /// Reference to the service client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        internal StorageAccountsOperations(MKIOClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            Client = client;
        }

        public StorageResponseSchema Create(StorageRequestSchema storage)
        {
            var task = Task.Run<StorageResponseSchema>(async () => await CreateAsync(storage));
            return task.GetAwaiter().GetResult();
        }

        public async Task<StorageResponseSchema> CreateAsync(StorageRequestSchema storage)
        {
            storage.Spec.Type = "Microsoft.Storage"; // needed
            string URL = Client.GenerateStorageApiUrl(storageApiUrl);
            string responseContent = await Client.CreateObjectPostAsync(URL, storage.ToJson());
            return JsonConvert.DeserializeObject<StorageResponseSchema>(responseContent, ConverterLE.Settings);
        }

        public List<StorageResponseSchema> List()
        {
            var task = Task.Run<List<StorageResponseSchema>>(async () => await ListAsync());
            return task.GetAwaiter().GetResult();
        }

        public async Task<List<StorageResponseSchema>> ListAsync()
        {
            string URL = Client.GenerateStorageApiUrl(storageApiUrl);
            string responseContent = await Client.GetObjectContentAsync(URL);
            return JsonConvert.DeserializeObject<StorageListResponseSchema>(responseContent, ConverterLE.Settings).Items;
        }

        public StorageResponseSchema Get(Guid storageAccountId)
        {
            var task = Task.Run<StorageResponseSchema>(async () => await GetAsync(storageAccountId));
            return task.GetAwaiter().GetResult();
        }

        public async Task<StorageResponseSchema> GetAsync(Guid storageAccountId)
        {
            string URL = Client.GenerateStorageApiUrl(storageSelectionApiUrl, storageAccountId.ToString());
            string responseContent = await Client.GetObjectContentAsync(URL);
            return JsonConvert.DeserializeObject<StorageResponseSchema>(responseContent, ConverterLE.Settings);
        }

        public void Delete(Guid storageAccountId)
        {
            Task.Run(async () => await DeleteAsync(storageAccountId));
        }

        public async Task DeleteAsync(Guid storageAccountId)
        {
            await StorageAccountOperationAsync(storageAccountId, HttpMethod.Delete);
        }

        public List<CredentialResponseSchema> ListCredentials(Guid storageAccountId)
        {
            var task = Task.Run<List<CredentialResponseSchema>>(async () => await ListCredentialsAsync(storageAccountId));
            return task.GetAwaiter().GetResult();
        }

        public async Task<List<CredentialResponseSchema>> ListCredentialsAsync(Guid storageAccountId)
        {
            string URL = Client.GenerateStorageApiUrl(storageListCredentialsApiUrl, storageAccountId.ToString());
            string responseContent = await Client.GetObjectContentAsync(URL);
            return JsonConvert.DeserializeObject<CredentialListReponseSchema>(responseContent, ConverterLE.Settings).Items;
        }

        public CredentialResponseSchema GetCredential(Guid storageAccountId, Guid credentialId)
        {
            var task = Task.Run<CredentialResponseSchema>(async () => await GetCredentialAsync(storageAccountId, credentialId));
            return task.GetAwaiter().GetResult();
        }

        public async Task<CredentialResponseSchema> GetCredentialAsync(Guid storageAccountId, Guid credentialId)
        {
            string URL = Client.GenerateStorageApiUrl(storageCredentialApiUrl, storageAccountId.ToString(), credentialId.ToString());
            string responseContent = await Client.GetObjectContentAsync(URL);
            return JsonConvert.DeserializeObject<CredentialResponseSchema>(responseContent, ConverterLE.Settings);
        }

        private async Task StorageAccountOperationAsync(Guid storageAccountId, HttpMethod httpMethod)
        {
            string URL = Client.GenerateStorageApiUrl(storageSelectionApiUrl, storageAccountId.ToString());
            await Client.ObjectContentAsync(URL, httpMethod);
        }
    }
}