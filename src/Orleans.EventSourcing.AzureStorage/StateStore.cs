using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.EventSourcing.AzureStorage
{
    public class StateStore : IStateStore
    {
        private readonly static SemaphoreSlim _syncLock = new SemaphoreSlim(1);

        private readonly string _connectionString;
        private readonly string _containerName;

        private CloudBlobContainer _container;

        public string Name { get; }

        public StateStore(string name, string connectionString, string containerName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }

        public async Task<StorableState> ReadAsync(string key)
        {
            var blob = await GetBlockBlobReferenceAsync(key).ConfigureAwait(false);

            try
            {
                var json = await blob.DownloadTextAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<StorableState>(json);
            }
            catch (StorageException ex) when (
                ex.RequestInformation.HttpStatusCode == 404 &&
                ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "BlobNotFound")
            {
                return null;
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        public async Task WriteAsync(string key, StorableState storableState)
        {
            var blob = await GetBlockBlobReferenceAsync(key).ConfigureAwait(false);
            var json = JsonConvert.SerializeObject(storableState);
            await blob.UploadTextAsync(json).ConfigureAwait(false);
        }

        private async Task<CloudBlockBlob> GetBlockBlobReferenceAsync(string blobName)
        {
            if (_container == null)
                await InitContainerReferenceAsync();

            return _container.GetBlockBlobReference(blobName);
        }

        private async Task InitContainerReferenceAsync()
        {
            await _syncLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_container == null)
                {
                    var account = CloudStorageAccount.Parse(_connectionString);
                    var client = account.CreateCloudBlobClient();
                    _container = client.GetContainerReference(_containerName);
                    await _container.CreateIfNotExistsAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }
    }
}
