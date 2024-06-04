﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using MK.IO;
using MK.IO.Models;

namespace Sample
{
    public class ProgramDemo
    {
        private const string TransformName = "CVQ720pTransform";
        private const string InputMP4FileName = @"Ignite.mp4";
        private const string BitmovinPlayer = "https://bitmovin.com/demos/stream-test?format={0}&manifest={1}";

        public static async Task SimpleEncodingAndPublishing()
        {
            /* you need to add an appsettings.json file with the following content:
           {
              "MKIOSubscriptionName": "yourMKIOsubscriptionname",
              "MKIOToken": "yourMKIOtoken",
              "TenantId" : "your Azure tenant ID " // optional
           }
          */

            Console.WriteLine("Sample that uploads a file in a new asset, does a video encoding to multiple bitrate with MK.IO and publish the output asset for clear streaming.");

            // Load settings from the appsettings.json file and check them
            IConfigurationRoot config = LoadAndCheckSettings();

            // Creating a unique suffix so that we don't have name collisions if you run the sample
            // multiple times without cleaning up.
            string uniqueId = MKIOClient.GenerateUniqueName(string.Empty);
            string inputAssetName = $"input-{uniqueId}";
            string outputAssetName = $"output-{uniqueId}";
            string jobName = $"job-{uniqueId}";
            string locatorName = $"locator-{uniqueId}";

            // MK.IO Client creation
            var client = new MKIOClient(config["MKIOSubscriptionName"]!, config["MKIOToken"]!);

            // Create a new input Asset and upload the specified local video file into it. We use the delete flag to delete the blob and container if we delete the asset in MK.IO
            _ = await CreateInputAssetAsync(client, config["StorageName"]!, config["TenantId"]!, inputAssetName, InputMP4FileName);

            // Output from the encoding Job must be written to an Asset, so let's create one. We use the delete flag to delete the blob and container if we delete the asset in MK.IO
            _ = await CreateOutputAssetAsync(client, config["StorageName"]!, outputAssetName, $"encoded asset from {inputAssetName} using {TransformName} transform");

            // Ensure that you have the desired encoding Transform. This is really a one time setup operation.
            _ = await CreateOrUpdateTransformAsync(client, TransformName);

            // Submit a job request to MK.IO to apply the specified Transform to a given input video.
            _ = await SubmitJobAsync(client, TransformName, jobName, inputAssetName, outputAssetName, InputMP4FileName);

            // Polls the status of the job and wait for it to finish.
            var encodingJob = await WaitForJobToFinishAsync(client, TransformName, jobName);

            if (encodingJob.Properties.State != JobState.Finished)
            {
                Console.WriteLine("Encoding job cancelled or in error.");
                await CleanIfUserAcceptsAsync(client, inputAssetName, outputAssetName, TransformName, jobName);
            }
            else
            {
                // Create a locator for clear streaming
                _ = await CreateStreamingLocatorAsync(client, outputAssetName, locatorName);

                // list the streaming endpoint(s) and propose creation if needed
                var createdEndpoint = await ListStreamingEndpointsAndCreateOneIfNeededAsync(client);

                // Display streaming paths and test player urls
                await ListStreamingUrlsAsync(client, locatorName);

                await CleanIfUserAcceptsAsync(client, inputAssetName, outputAssetName, TransformName, jobName, locatorName, createdEndpoint);
                Console.WriteLine("Press Enter to close the app.");
            }
        }

        /// <summary>
        /// do the settings loading and checking
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static IConfigurationRoot LoadAndCheckSettings()
        {
            // Build a config object, using env vars and JSON providers.
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            // Checking settings
            if (string.IsNullOrEmpty(config["MKIOSubscriptionName"]) || string.IsNullOrEmpty(config["MKIOToken"]) || string.IsNullOrEmpty(config["StorageName"]))
            {
                Console.WriteLine("Missing MKIOSubscriptionName, MKIOToken, or StorageName in configuration file.");
                throw new Exception("Missing mandatory configuration settings.");
            }

            Console.WriteLine($"Using '{config["MKIOSubscriptionName"]!}' MK.IO subscription.");
            return config;
        }

        /// <summary>
        /// Creates a new input Asset and uploads the specified local video file into it.
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="storageName">The storage name.</param>
        /// <param name="tenantId">The Azure Tenant Id</param>
        /// <param name="assetName">The asset name.</param>
        /// <param name="fileToUpload">The file you want to upload into the asset.</param>
        /// <returns></returns>
        private static async Task<AssetSchema> CreateInputAssetAsync(MKIOClient client, string storageName, string tenantId, string assetName, string fileToUpload)
        {
            // Create an input asset
            var inputAsset = await client.Assets.CreateOrUpdateAsync(
                assetName,
                null,
                storageName!,
                $"input asset {fileToUpload}",
                AssetContainerDeletionPolicyType.Delete,
                null,
                new Dictionary<string, string> { { "typeAsset", "source" } }
            );
            Console.WriteLine($"Input asset '{inputAsset.Name}' created.");

            // Create an interactive browser credential which will use the system authentication broker
            var blobContainerClient = new BlobContainerClient(
                new Uri($"https://{inputAsset.Properties.StorageAccountName}.blob.core.windows.net/{inputAsset.Properties.Container}"),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions()
                {
                    TenantId = tenantId
                }
                )
                );

            // User or app must have Storage Blob Data Contributor on the account for the upload to work!
            // Upload a blob (e.g., from a local file)
            var blobClient = blobContainerClient.GetBlobClient(fileToUpload);
            await blobClient.UploadAsync(fileToUpload);
            Console.WriteLine($"File '{fileToUpload}' uploaded to input asset.");
            return inputAsset;
        }

        /// <summary>
        /// Creates an ouput asset. The output from the encoding Job must be written to an Asset.
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="storageName">The storage name.</param>
        /// <param name="assetName">The asset name.</param>
        /// <param name="description">The description of the asset.</param>
        /// <returns></returns>
        private static async Task<AssetSchema> CreateOutputAssetAsync(MKIOClient client, string storageName, string assetName, string description)
        {
            // Create an empty output asset

            var outputAsset = await client.Assets.CreateOrUpdateAsync(
                assetName,
                null,
                storageName!,
                description,
                AssetContainerDeletionPolicyType.Delete,
                null,
                new Dictionary<string, string> { { "typeAsset", "encoded" } }
                );
            Console.WriteLine($"Output asset '{assetName}' created.");

            return outputAsset;
        }

        /// <summary>
        /// Create or update the Transform
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="transformName">The transform name.</param>
        /// <returns></returns>
        private static async Task<TransformSchema> CreateOrUpdateTransformAsync(MKIOClient client, string transformName)
        {
            // Create or update a transform for CVQ encoding
            var transform = await client.Transforms.CreateOrUpdateAsync(transformName, new TransformProperties
            {
                Description = "Encoding with H264MultipleBitrate720pWithCVQ preset",
                Outputs = new List<TransformOutput>
                {
                    new() {
                        Preset = new BuiltInStandardEncoderPreset(EncoderNamedPreset.H264MultipleBitrate720pWithCVQ),
                        RelativePriority = TransformOutputPriorityType.Normal
                    }
                }
            });
            Console.WriteLine($"Transform '{transform.Name}' created/updated.");
            return transform;
        }

        /// <summary>
        /// Submits a request to MK.IO to apply the specified Transform to a given input video.
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="transformName">The transform name.</param>
        /// <param name="jobName">The job name.</param>
        /// <param name="inputAssetName">The input asset name.</param>
        /// <param name="outputAssetName">The output asset name.</param>
        /// <param name="fileName">The filename in the input asset name.</param>
        /// <returns></returns>
        private static async Task<JobSchema> SubmitJobAsync(MKIOClient client, string transformName, string jobName, string inputAssetName, string outputAssetName, string fileName)
        {
            // Create the encoding job
            var encodingJob = await client.Jobs.CreateAsync(
                transformName,
                jobName,
                new JobProperties
                {
                    Description = $"My job which encodes '{inputAssetName}' to '{outputAssetName}' with '{transformName}' transform.",
                    Priority = JobPriorityType.Normal,
                    Input = new JobInputAsset(
                       inputAssetName,
                       [
                           fileName
                       ]),
                    Outputs =
                    [
                       new JobOutputAsset()
                       {
                           AssetName= outputAssetName
                       }
                    ]
                }
                );
            Console.WriteLine($"Encoding job '{encodingJob.Name}' submitted.");
            return encodingJob;
        }

        /// <summary>
        /// Create a streaming locator on the asset
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="outputAssetName">The output asset name.</param>
        /// <param name="locatorName">The locator name.</param>
        /// <returns></returns>
        private static async Task<StreamingLocatorSchema> CreateStreamingLocatorAsync(MKIOClient client, string outputAssetName, string locatorName)
        {
            return await client.StreamingLocators.CreateAsync(
                 locatorName,
                 new StreamingLocatorProperties
                 {
                     AssetName = outputAssetName,
                     StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly
                 });
        }

        /// <summary>
        /// List the streaming endpoint(s) and propose to create one if none
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <returns>The name of the streaming endpoint created, otherwise null</returns>
        private static async Task<string?> ListStreamingEndpointsAndCreateOneIfNeededAsync(MKIOClient client)
        {
            string? createdStreamingEndpointName = null;
            var streamingEndpoints = await client.StreamingEndpoints.ListAsync();
            if (streamingEndpoints.Any())
            {
                Console.WriteLine("Streaming endpoints:");
                foreach (var streamingEndpoint in streamingEndpoints)
                {
                    Console.WriteLine($"   {streamingEndpoint.Name} ({streamingEndpoint.Properties.ResourceState})");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("No streaming endpoint found. Do you want to create one and start it? (Y/N)");
                var response = Console.ReadLine();
                if (response == "Y" || response == "y")
                {
                    var sub = await client.Account.GetSubscriptionAsync();
                    var locs = await client.Account.ListAllLocationsAsync();
                    var locationToUse = locs.FirstOrDefault(l => l.Metadata.Id == sub.Spec.LocationId);

                    if (locationToUse != null)
                    {
                        var streamingEndpoint = await client.StreamingEndpoints.CreateAsync(
                           MKIOClient.GenerateUniqueName("endpoint"),
                           locationToUse.Metadata.Name,
                           new StreamingEndpointProperties
                           {
                               Description = "Streaming endpoint created by sample"
                           },
                           true
                           );
                        createdStreamingEndpointName = streamingEndpoint.Name;
                        Console.WriteLine($"Streaming endpoint '{streamingEndpoint.Name}' created and starting.");
                    }
                    else
                    {
                        Console.WriteLine("Error. No location found. Cannot create the streaming endpoint.");
                    }
                    Console.WriteLine();
                }
            }
            return createdStreamingEndpointName;
        }

        /// <summary>
        /// List the streaming Urls for a specified locator name
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="locatorName">The locator name.</param>
        /// <returns></returns>
        private static async Task ListStreamingUrlsAsync(MKIOClient client, string locatorName)
        {
            // list Streaming Endpoints
            var streamingEndpoints = await client.StreamingEndpoints.ListAsync();

            // List the streaming Url
            var paths = await client.StreamingLocators.ListUrlPathsAsync(locatorName);
            Console.WriteLine($"Streaming paths for locator '{locatorName}':");
            foreach (var path in paths.StreamingPaths)
            {
                Console.WriteLine($"   Streaming protocol : {path.StreamingProtocol}");
                foreach (var p in path.Paths)
                {
                    foreach (var se in streamingEndpoints)
                    {
                        Console.WriteLine($"      Url : https://{se.Properties.HostName}{p}");
                        Console.WriteLine($"      Test player url: " + string.Format(BitmovinPlayer, path.StreamingProtocol.ToString().ToLower(), Uri.EscapeDataString("https://" + se.Properties.HostName + p)));
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Polls MK.IO for the status of the Job.
        /// </summary>
        /// <param name="client">The MK.IO client.</param>
        /// <param name="transformName">The transform name.</param>
        /// <param name="jobName">The job name.</param>
        /// <returns></returns>
        private static async Task<JobSchema> WaitForJobToFinishAsync(MKIOClient client, string transformName, string jobName)
        {
            const int SleepIntervalMs = 10 * 1000;
            JobSchema job;

            do
            {
                await Task.Delay(SleepIntervalMs);
                job = await client.Jobs.GetAsync(transformName, jobName);
                Console.WriteLine(job.Properties.State + (job.Properties.Outputs.First().Progress != null ? $" {job.Properties.Outputs.First().Progress}%" : string.Empty));
            }
            while (job.Properties.State == JobState.Queued || job.Properties.State == JobState.Scheduled || job.Properties.State == JobState.Processing);

            return job;
        }

        /// <summary>
        /// Clean the created resources if user accepts
        /// </summary>
        /// <param name="client"></param>
        /// <param name="inputAssetName"></param>
        /// <param name="outputAssetName"></param>
        /// <param name="transformName"></param>
        /// <param name="jobName"></param>
        /// <param name="locatorName"></param>
        /// <returns></returns>
        private static async Task CleanIfUserAcceptsAsync(MKIOClient client, string inputAssetName, string outputAssetName, string transformName, string jobName, string? locatorName = null, string? streamingEndpoint = null)
        {
            Console.WriteLine("Do you want to clean the created resources (assets, job, etc) ? (Y/N)");
            var response = Console.ReadLine();
            if (response == "Y" || response == "y")
            {
                try
                {
                    await client.Assets.DeleteAsync(inputAssetName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error deleting asset '{inputAssetName}'. Error: {ex.Message}");
                }

                try
                {
                    await client.Assets.DeleteAsync(outputAssetName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting asset '{outputAssetName}'. Error: {ex.Message}");
                }

                try
                {
                    await client.Jobs.DeleteAsync(transformName, jobName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting asset '{outputAssetName}'. Error: {ex.Message}");
                }

                try
                {
                    await client.Transforms.DeleteAsync(transformName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting transform '{transformName}'. Error: {ex.Message}");
                }

                if (locatorName != null)
                    try
                    {
                        await client.StreamingLocators.DeleteAsync(locatorName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting locator '{locatorName}'. Error: {ex.Message}");
                    }
                if (streamingEndpoint != null)
                    try
                    {
                        await client.StreamingEndpoints.DeleteAsync(streamingEndpoint);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting streaming endpoint '{streamingEndpoint}'. Error: {ex.Message}");
                    }
                Console.WriteLine("Cleaning done.");
            }
        }
    }
}
