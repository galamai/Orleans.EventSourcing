using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.EventSourcing.AzureStorage
{
    public class EventStore : IEventStore
    {
        private const string RowKeyVersionUpperLimit = "9999999999999999999";
        private const string UnpublishedRowKeyPrefix = "Unpublished_";
        private const string UnpublishedRowKeyPrefixUpperLimit = "Unpublished`";

        private readonly static SemaphoreSlim _syncLock = new SemaphoreSlim(1);

        private readonly string _connectionString;
        private readonly string _tableName;

        private CloudTable _table;

        public string Name { get; }

        public EventStore(string name, string connectionString, string tableName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        public Task<Slice> ReadAsync(string key, long version)
        {
            return ReadAsync(key, GetRowKey(version), RowKeyVersionUpperLimit);
        }

        public Task<Slice> ReadUnpublishedAsync(string key)
        {
            return ReadAsync(key, UnpublishedRowKeyPrefix, UnpublishedRowKeyPrefixUpperLimit);
        }

        public async Task WriteAsync(string key, IEnumerable<StorableEvent> evetList)
        {
            if (_table == null)
                await InitTableReferenceAsync();

            var batch = new TableBatchOperation();

            foreach (var storableEvent in evetList)
            {
                var rowKey = GetRowKey(storableEvent.Version);
                var type = storableEvent.Type;

                batch.Add(TableOperation.Insert(new EventTableEntity(storableEvent.Payload)
                {
                    PartitionKey = key,
                    RowKey = rowKey,
                    Type = type
                }));
                batch.Add(TableOperation.Insert(new EventTableEntity(storableEvent.Payload)
                {
                    PartitionKey = key,
                    RowKey = UnpublishedRowKeyPrefix + rowKey,
                    Type = type
                }));
            }

            await _table.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        public async Task DeletePublishedAsync(string key, IEnumerable<long> versionList)
        {
            if (_table == null)
                await InitTableReferenceAsync();

            var batch = new TableBatchOperation();

            foreach (var version in versionList)
            {
                batch.Add(TableOperation.Delete(new TableEntity()
                {
                    PartitionKey = key,
                    RowKey = UnpublishedRowKeyPrefix + GetRowKey(version),
                    ETag = "*"
                }));
            }

            await _table.ExecuteBatchAsync(batch).ConfigureAwait(false);
        }

        private async Task<Slice> ReadAsync(string key, string startRowKey, string endRowKey)
        {
            if (_table == null)
                await InitTableReferenceAsync();

            var condition = GeneratePartitionKeyWithRowKeySliceFilter(key, startRowKey, endRowKey);
            var query = new TableQuery<EventTableEntity>().Where(condition);
            var tableQueryResult = await _table.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false);
            return new Slice(
                tableQueryResult
                    .Select(x =>
                        new StorableEvent(
                            version: uint.Parse(x.RowKey),
                            type: x.Type,
                            payload: x.GetData()))
                    .ToList(),
            tableQueryResult.ContinuationToken != null);
        }

        private async Task InitTableReferenceAsync()
        {
            await _syncLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_table == null)
                {
                    var account = CloudStorageAccount.Parse(_connectionString);
                    var tableClient = account.CreateCloudTableClient();
                    _table = tableClient.GetTableReference(_tableName);
                    await _table.CreateIfNotExistsAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private string GetRowKey(long version)
        {
            return version.ToString("D19");
        }

        private string GeneratePartitionKeyWithRowKeySliceFilter(string partitionKey, string startRowKey, string endRowKey)
        {
            return TableQuery.CombineFilters(
                GeneratePartitionKeyFilter(partitionKey),
                TableOperators.And,
                GenerateRowKeySliceFilter(startRowKey, endRowKey));
        }

        private string GeneratePartitionKeyFilter(string partitionKey)
        {
            return TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
        }

        private string GenerateRowKeySliceFilter(string startRowKey, string endRowKey)
        {
            var condition1 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, startRowKey);
            var condition2 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, endRowKey);
            return TableQuery.CombineFilters(condition1, TableOperators.And, condition2);
        }
    }
}
