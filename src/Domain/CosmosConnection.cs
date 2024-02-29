namespace SubscriptionCleanupUtils.Domain
{
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using SubscriptionCleanupUtils.Models.Cosmos;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class, not fully generalized, to read the product Cosmos DB and acquire specific 
    /// types of items. In our case Resources and Data Partitions. 
    /// 
    /// Also allows limited flexibility to update these same records. 
    /// </summary>
    internal class CosmosConnection
    {
        private const string QUERY = "SELECT * FROM c WHERE c.oepResourceId = '{0}'";

        private CosmosDetail Details { get; set; }

        private ITokenProvider TokenProvider { get; set; }
        private CosmosClient? Client { get; set; } = null;
        private Container? Resources { get; set; } = null;
        private Container? DataPartitions { get; set; } = null;

        public CosmosConnection(ITokenProvider tokenProvider, CosmosDetail details)
        {
            this.Details = details;
            this.TokenProvider = tokenProvider;
        }

        public void Connect()
        {
            if(this.Client == null)
            {
                string cosmosKey = CosmosKeyFetcher.GetCosmosDbPrimaryMasterKeyAsync(
                    this.TokenProvider,
                    this.Details.Id
                ).Result;

                this.Client = new CosmosClient(this.Details.Endpoint, cosmosKey);

                var database = this.Client.GetDatabase(this.Details.CosmosDatabase);
                this.Resources = database.GetContainer("OEPResource");
                this.DataPartitions = database.GetContainer("DataPartitions");
            }
        }

        public OEPResourceEntity? GetResource(string resourceId)
        {
            if (this.Resources != null)
            {
                QueryDefinition querydefinition = new QueryDefinition(string.Format(QUERY, resourceId));
                List<OEPResourceEntity> results =
                    (List<OEPResourceEntity>)this.QueryItemsAsync<OEPResourceEntity>(
                        this.Resources,
                        querydefinition).Result;

                return results.FirstOrDefault();
            }
            return null;
        }

        public async Task<string> UpsertResource(OEPResourceEntity entity)
        {
            var entity_str = JsonConvert.SerializeObject(entity, Formatting.Indented);
            Microsoft.Azure.Cosmos.PartitionKey key = new Microsoft.Azure.Cosmos.PartitionKey(entity.PartitionKey);

            string response = string.Empty;

            if (this.Resources != null)
            {
#pragma warning disable CS0168 
                try
                {
                    response = await this.UpsertSerializedItemAsync(this.Resources, entity_str, key);
                }
                catch (Exception ex)
                {
                    await Task.Delay(500);
                    response = await this.UpsertSerializedItemAsync(this.Resources, entity_str, key);
                }
#pragma warning restore CS0168 
            }

            return response;
        }
        public async Task<string> UpsertDataPartition(DataPartitionsEntity entity)
        {
            var entity_str = JsonConvert.SerializeObject(entity, Formatting.Indented);
            Microsoft.Azure.Cosmos.PartitionKey key = new Microsoft.Azure.Cosmos.PartitionKey(entity.PartitionKey);

            string response = string.Empty;

            if (this.DataPartitions != null)
            {
#pragma warning disable CS0168 
                try
                {
                    response = await this.UpsertSerializedItemAsync(this.DataPartitions, entity_str, key);
                }
                catch (Exception ex)
                {
                    await Task.Delay(500);
                    response = await this.UpsertSerializedItemAsync(this.DataPartitions, entity_str, key);
                }
#pragma warning restore CS0168 
            }

            return response;
        }

        public List<DataPartitionsEntity> GetDataPartitions(string resourceId)
        {
            QueryDefinition querydefinition = new QueryDefinition(string.Format(QUERY, resourceId));
#pragma warning disable CS8604 
            List<DataPartitionsEntity> results = 
                (List<DataPartitionsEntity>)this.QueryItemsAsync<DataPartitionsEntity>(
                    this.DataPartitions, 
                    querydefinition).Result;
#pragma warning restore CS8604 

            return  results;
        }

        private async Task<IList<T>> QueryItemsAsync<T>(Container container, QueryDefinition querydefinition)
        {
            this.Connect();

            FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(querydefinition);
            List<T> results = new List<T>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                results.AddRange(currentResultSet);
            }

            return results;
        }

        private async Task<string> UpsertSerializedItemAsync(
            Container container,
            string entity,
            PartitionKey partitionKey,
            ItemRequestOptions? requestOptions = null)
        {
            string return_value = string.Empty;

            try
            {
                var stream = ToStream(entity);
                var responseMessage = await container.UpsertItemStreamAsync(stream, partitionKey, requestOptions);
                responseMessage.EnsureSuccessStatusCode();
                return_value = FromStream(responseMessage.Content);
            }
            catch (Exception ex)
            {
                Exception ex_extended = new Exception(container.Id, ex);
                throw ex_extended;
            }
            return return_value;
        }

        private static Stream ToStream(string input)
        {
            var byteArray = Encoding.ASCII.GetBytes(input);
            return new MemoryStream(byteArray);
        }

        private static string FromStream(Stream input)
        {
            var reader = new StreamReader(input);
            return reader.ReadToEnd();
        }
    }
}
