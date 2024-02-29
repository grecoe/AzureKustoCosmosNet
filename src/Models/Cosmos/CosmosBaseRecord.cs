namespace SubscriptionCleanupUtils.Models.Cosmos
{
#pragma warning disable CS8618

    using Newtonsoft.Json;

    public class CosmosBaseRecord
    {
        /// <summary>
        /// The record Identity.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public virtual string Id { get; set; }


        /// <summary>
        /// Gets or sets partition key.
        /// </summary>
        [JsonProperty(PropertyName = "partitionKey")]
        public virtual string PartitionKey { get; set; }


        /// <summary>
        /// Gets or sets created time.
        /// </summary>
        [JsonProperty(PropertyName = "created", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets lastModified time.
        /// </summary>
        [JsonProperty(PropertyName = "lastModified", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastModifiedTime { get; set; } = null;

        /// <summary>
        /// Gets or sets ETAG.
        /// </summary>
        [JsonProperty(PropertyName = "_etag", NullValueHandling = NullValueHandling.Ignore)]
        public string CosmosDbEntityTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp value.
        /// </summary>
        [JsonProperty(PropertyName = "_ts")]
        public long CosmosDbTimeStamp { get; set; } = 0;

        /// <summary>
        /// Gets or sets the resource Id value.
        /// </summary>
        [JsonProperty(PropertyName = "_rid")]
        public string CosmosDbResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the self value.
        /// </summary>
        [JsonProperty(PropertyName = "_self")]
        public string CosmosDbSelf { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        [JsonProperty(PropertyName = "_attachments")]
        public string CosmosDbAttachment { get; set; }

        // Define your partition key in deriving classes
        /*
     "_rid": "pdEOAJjBxzABAAAAAAAAAA==",
     "_self": "dbs/pdEOAA==/colls/pdEOAJjBxzA=/docs/pdEOAJjBxzABAAAAAAAAAA==/",
     "_etag": "\"c10781cd-0000-0100-0000-64f4b7360000\"",
     "_attachments": "attachments/",
     "_ts": 1693759286        
         */
    }

#pragma warning restore CS8618
}
