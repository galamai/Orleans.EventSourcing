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

        public StateStore(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
        }

        public async Task<(TState state, long version)> ReadAsync<TState>(string key)
        {
            var blob = await GetBlockBlobReferenceAsync(key).ConfigureAwait(false);

            try
            {
                var json = await blob.DownloadTextAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<(TState state, uint version)>(json);
            }
            catch (StorageException ex) when (
                ex.RequestInformation.HttpStatusCode == 404 &&
                ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "BlobNotFound")
            {
                return (default(TState), default(uint));
            }
            catch (SerializationException)
            {
                return (default(TState), default(uint));
            }
        }

        public async Task WriteAsync<TState>(string key, (TState state, long version) serialState)
        {
            var blob = await GetBlockBlobReferenceAsync(key).ConfigureAwait(false);
            var json = JsonConvert.SerializeObject(serialState);
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
