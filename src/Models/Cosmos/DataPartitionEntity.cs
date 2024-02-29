namespace SubscriptionCleanupUtils.Models.Cosmos
{
#pragma warning disable CS8618

    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This Class represents a Data Plane Info entity.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public class DataPartitionsEntity : CosmosBaseRecord
    {
        private DateTime? createdOnUTC;

        /// <inheritdoc/>
        public override string PartitionKey => this.OEPResourceId;

        /// <summary>
        /// Gets or sets OEP resource Id.
        /// </summary>
        [JsonProperty(PropertyName = "oepResourceId")]
        public string OEPResourceId { get; set; }

        /// <summary>
        /// Gets or sets data partition name.
        /// </summary>
        [JsonProperty(PropertyName = "dataPartitionName")]
        public string DataPartitionName { get; set; }

        /// <summary>
        /// Gets or sets customer tenant Id.
        /// </summary>
        [JsonProperty(PropertyName = "customerTenantId")]
        public string CustomerTenantId { get; set; }

        /// <summary>
        /// Gets or sets customer subscription Id.
        /// </summary>
        [JsonProperty(PropertyName = "customerSubscriptionId")]
        public string CustomerSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets managed resource group name.
        /// </summary>
        [JsonProperty(PropertyName = "managedRGName")]
        public string ManagedRGName { get; set; }

        /// <summary>
        /// Gets or sets provisioning status.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningState")]
        public ProvisioningState ProvisioningState { get; set; }

        /// <summary>
        /// Gets or sets created datetime.
        /// </summary>
        [JsonProperty(PropertyName = "createdOn")]
        public DateTime? CreatedOnUTC
        {
            get => this.createdOnUTC;
            set => this.createdOnUTC = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets entity version.
        /// </summary>
        [JsonProperty(PropertyName = "entityVersion")]
        public string EntityVersion { get; set; }

        /// <summary>
        /// DataPartitionResourceNamesSuffix.
        /// </summary>
        [JsonProperty(PropertyName = "dataPartitionResourceNamesSuffix")]
        public string DataPartitionResourceNamesSuffix { get; set; }
    }

#pragma warning restore CS8618
}
