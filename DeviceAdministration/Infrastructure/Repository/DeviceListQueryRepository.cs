﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.DeviceAdmin.Infrastructure.Models;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Configurations;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Helpers;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.DeviceAdmin.Infrastructure.Repository
{
    public class DeviceListQueryRepository : IDeviceListQueryRepository
    {
        private readonly string _storageAccountConnectionString;
        private readonly IAzureTableStorageClient _azureTableStorageClient;
        private readonly string _queryTableName = "QueryList";
        private const string _queryTablePartitionKey = "query";

        public DeviceListQueryRepository (IConfigurationProvider configurationProvider, IAzureTableStorageClientFactory tableStorageClientFactory)
        {
            _storageAccountConnectionString = configurationProvider.GetConfigurationSettingValue("device.StorageConnectionString");
            string queryTableName = configurationProvider.GetConfigurationSettingValueOrDefault("DeviceListQueryTableName", _queryTableName);
            _azureTableStorageClient = tableStorageClientFactory.CreateClient(_storageAccountConnectionString, queryTableName);
        }

        public async Task<bool> CheckQueryNameAsync(string name)
        {
            TableQuery<DeviceListQueryTableEntity> query = new TableQuery<DeviceListQueryTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name));
            var entities = await _azureTableStorageClient.ExecuteQueryAsync(query);
            return entities.Count() > 0;
        }

        public async Task<DeviceListQuery> GetQueryAsync(string name)
        {
            TableQuery<DeviceListQueryTableEntity> query = new TableQuery<DeviceListQueryTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name));
            var entities = await _azureTableStorageClient.ExecuteQueryAsync(query);
            if (entities.Count() == 0) return null;
            var entity = entities.First();
            return new DeviceListQuery
            {
                Name = entity.Name,
                Filters = JsonConvert.DeserializeObject<List<FilterInfo>>(entity.Filters),
                Sql = entity.Sql,
                IsAdvanced = entity.IsAdvanced,
            };
        }

        public async Task<bool> SaveQueryAsync(DeviceListQuery query, bool force = false)
        {
            // if force = false and query already exists, count>0
            if (!force && await CheckQueryNameAsync(query.Name))
            {
                return false;
            }
            string filters = JsonConvert.SerializeObject(query.Filters, Formatting.None, new StringEnumConverter());
            DeviceListQueryTableEntity entity = new DeviceListQueryTableEntity(_queryTablePartitionKey, query.Name);
            entity.ETag = "*";
            entity.Filters = filters;
            entity.SortColumn = query.SortColumn;
            entity.SortOrder = query.SortOrder.ToString();
            entity.Sql = query.Sql;
            entity.IsAdvanced = query.IsAdvanced;
            var result = await _azureTableStorageClient.DoTableInsertOrReplaceAsync(entity, BuildQueryModelFromEntity);
            return (result.Status == TableStorageResponseStatus.Successful);
        }

        public async Task<bool> TouchQueryAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            DeviceListQueryTableEntity entity = new DeviceListQueryTableEntity(_queryTablePartitionKey, name);
            var result = await _azureTableStorageClient.DoTouchAsync(entity, BuildQueryModelFromEntity);
            return result.Status == TableStorageResponseStatus.Successful;
        }

        public async Task<bool> DeleteQueryAsync(string name)
        {
            DeviceListQueryTableEntity entity = new DeviceListQueryTableEntity(_queryTablePartitionKey, name);
            entity.ETag = "*";
            var result = await _azureTableStorageClient.DoDeleteAsync(entity, BuildQueryModelFromEntity);
            return (result.Status == TableStorageResponseStatus.Successful);
        }

        public async Task<IEnumerable<DeviceListQuery>> GetRecentQueriesAsync(int Max = 20)
        {
            TableQuery<DeviceListQueryTableEntity> query = new TableQuery<DeviceListQueryTableEntity>();
            var entities = await _azureTableStorageClient.ExecuteQueryAsync(query);
            var ordered = entities.OrderByDescending(e => e.Timestamp);
            if (Max > 0)
            {
                return ordered.Take(Max).Select(e => BuildQueryModelFromEntity(e));
            }
            else
            {
                return ordered.Select(e => BuildQueryModelFromEntity(e));
            }
        }

        public async Task<IEnumerable<string>> GetQueryNameListAsync()
        {
            TableQuery<DeviceListQueryTableEntity> query = new TableQuery<DeviceListQueryTableEntity>();
            var entities = await _azureTableStorageClient.ExecuteQueryAsync(query);
            var ordered = entities.OrderBy(e => e.Name);
            return ordered.Select(e => e.Name);
        }

        private DeviceListQuery BuildQueryModelFromEntity(DeviceListQueryTableEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            List<FilterInfo> filters = new List<FilterInfo>();
            try
            {
                filters = JsonConvert.DeserializeObject<List<FilterInfo>>(entity.Filters);
            }
            catch (Exception)
            {
                Trace.TraceError("Can not deserialize filters to Json object", entity.Filters);
            }

            QuerySortOrder order;
            if (!Enum.TryParse(entity.SortOrder, true, out order))
            {
                order = QuerySortOrder.Descending;
            }

            return new DeviceListQuery
            {
                Name = entity.Name,
                Filters = filters,
                SortColumn = entity.SortColumn,
                SortOrder = order,
                Sql = entity.Sql,
                IsAdvanced = entity.IsAdvanced,
            };
        }
    }
}