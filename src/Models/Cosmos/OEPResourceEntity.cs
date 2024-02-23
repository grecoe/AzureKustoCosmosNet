namespace SubscriptionCleanupUtils.Models.Cosmos
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Core.Networking;

    using Newtonsoft.Json;

    /// <summary>
    /// This Class represents a OEP resource Info entity.
    /// </summary>
    public class OEPResourceEntity : CosmosBaseRecord
    {
        private DateTime? createdOnUTC;

        /// <inheritdoc/>
        public override string PartitionKey => this.CustomerSubscriptionId;

        /// <inheritdoc/>
        public override string Id => this.OEPResourceId;

        /// <summary>
        /// Gets or sets customer subscription Id.
        /// </summary>
        [JsonProperty(PropertyName = "customerSubscriptionId")]
        public string CustomerSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets customer tenant Id.
        /// </summary>
        [JsonProperty(PropertyName = "customerTenantId")]
        public string CustomerTenantId { get; set; }

        /// <summary>
        /// Gets or sets resource name.
        /// </summary>
        [JsonProperty(PropertyName = "resourceName")]
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets resource group name.
        /// </summary>
        [JsonProperty(PropertyName = "resourceGroupName")]
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets resource location.
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets attribute name.
        /// </summary>
        [JsonProperty(PropertyName = "attributeName")]
        public string AttributeName { get; set; }

        /// <summary>
        /// Gets or sets friendly name.
        /// </summary>
        [JsonProperty(PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets OEP resource Id.
        /// </summary>
        [JsonProperty(PropertyName = "oepResourceId")]
        public string OEPResourceId { get; set; }

        /// <summary>
        /// Gets or sets DNS name.
        /// </summary>
        [JsonProperty(PropertyName = "dnsName")]
        public string DNSName { get; set; }

        /// <summary>
        /// Gets or sets provisioning state.
        /// </summary>
        [JsonProperty(PropertyName = "provisioningState")]
        public ProvisioningState ProvisioningState { get; set; }

        /// <summary>
        /// Gets or sets sku.
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; } = "Standard";

        /// <summary>
        /// Gets or sets osdu version.
        /// </summary>
        [JsonProperty(PropertyName = "osduVersion")]
        public string OSDUVersion { get; set; }

        /// <summary>
        /// Gets or sets auth app Id.
        /// </summary>
        [JsonProperty(PropertyName = "authAppId")]
        public string AuthAppId { get; set; }

        /// <summary>
        /// Gets or sets tier.
        /// </summary>
        [JsonProperty(PropertyName = "tier")]
        public string Tier { get; set; } = "Standard";

        /// <summary>
        /// Gets or sets tags.
        /// </summary>
        [JsonProperty(PropertyName = "tagsDict")]
        public IDictionary<string, string> TagsDict { get; set; }

        /// <summary>
        /// Gets or sets feature version.
        /// </summary>
        [JsonProperty(PropertyName = "featureVersion")]
        public string FeatureVersion { get; set; }

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
        /// Gets or sets entity version.
        /// </summary>
        [JsonProperty(PropertyName = "OEPRelease")]
        public string OEPRelease { get; set; }

        /// <summary>
        /// Gets or sets the managed identity information of the resrouce.
        /// </summary>
        [JsonProperty(PropertyName = "managedIdentity")]
        public ManagedIdentity ManagedIdentity { get; set; }

        /// <summary>
        /// Gets or sets the Encryption properties.
        /// </summary>
        [JsonProperty(PropertyName = "encryption")]
        public Encryption Encryption { get; set; }

        /// <summary>
        /// Gets or sets the Identity URL.
        /// </summary>
        [JsonProperty(PropertyName = "IdentityUrl")]
        public string IdentityUrl { get; set; }

        /// <summary>
        /// Gets or sets the HOBOV2 delegation information of the resource.
        /// TODO  get msiTenantId, internalId, resourceId, delegationId.
        /// </summary>
        [JsonProperty(PropertyName = "delegated_resources")]
        public DelegatedHoboV2Properties DelegatedHoboV2Properties { get; set; }

        /// <summary>
        /// Gets or sets the toggle status for public network access.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "publicNetworkAccess")]
        public PublicNetworkAccess PublicNetworkAccess { get; set; } = PublicNetworkAccess.Enabled;

        /// <summary>
        /// Gets or sets a value indicating whether encryption for cosmos db is enabled.
        /// </summary>
        [JsonProperty(PropertyName = "isCosmosDBEncryptionEnabled")]
        public bool IsCosmosDBEncryptionEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether encryption for storage account is enabled.
        /// </summary>
        [JsonProperty(PropertyName = "isStorageAccountEncryptionEnabled")]
        public bool IsStorageAccountEncryptionEnabled { get; set; }

        /// <summary>
        /// Gets or sets CorsRule
        /// </summary>
        [JsonProperty(PropertyName = "corsRules")]
        public List<CorsRules> CorsRules { get; set; }

        /// <summary>
        /// Gets or sets falg to indicater whether BCDR on the instance is enabled or not. Deprecated now.
        /// </summary>
        [ObsoleteAttribute("This field is deprecated now. Use SKU to check the BCDR status instead.", false)]
        [JsonProperty(PropertyName = "isBCDREnabled")]
        public bool? IsBCDREnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the status of provisioning in backup region.
        /// </summary>
        [JsonProperty(PropertyName = "backupRegionProvisioningState")]
        public ProvisioningState BackupRegionProvisioningState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the platform nodeOS image.
        /// </summary>
        [JsonProperty(PropertyName = "nodeOSImage")]
        public string NodeOSImage { get; set; }

        /// <summary>
        /// Gets or sets a eds metadata value
        /// </summary>
        [JsonProperty(PropertyName = "edsMetadata")]
        public EdsProperties EdsMetadata { get; set; }
    }
}
