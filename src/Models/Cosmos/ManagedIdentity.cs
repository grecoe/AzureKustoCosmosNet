namespace SubscriptionCleanupUtils.Models.Cosmos
{
#pragma warning disable CS8618

    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverageAttribute]
    public class UserAssignedIdentityProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAssignedIdentityProperties"/> class.
        /// </summary>
        /// <param name="principalId">principalId.</param>
        /// <param name="clientId">clientId.</param>
        public UserAssignedIdentityProperties(string principalId, string clientId)
        {
            this.PrincipalId = principalId;
            this.ClientId = clientId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAssignedIdentityProperties"/> class.
        /// </summary>
        public UserAssignedIdentityProperties()
        {
        }

        /// <summary>
        /// Gets or sets setup managed identity Id.
        /// </summary>
        [JsonProperty(
            PropertyName = "principalId",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets setup managed identity Id.
        /// </summary>
        [JsonProperty(
            PropertyName = "clientId",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientId { get; set; }
    }

    /// <summary>
    /// managed identity details .
    /// </summary>
    [ExcludeFromCodeCoverageAttribute]
    public class ManagedIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentity"/> class.
        /// </summary>
        /// <param name="type">managed idenity type.</param>
        public ManagedIdentity(string type)
        {
            this.Type = type;
            this.PrincipalId = string.Empty;
            this.TenantId = string.Empty;
            this.UserAssignedIdentities = new Dictionary<string, UserAssignedIdentityProperties>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentity"/> class.
        /// </summary>
        public ManagedIdentity()
        {
            this.Type = string.Empty;
            this.PrincipalId = string.Empty;
            this.TenantId = string.Empty;
            this.UserAssignedIdentities = new Dictionary<string, UserAssignedIdentityProperties>();
        }

        /// <summary>
        /// Gets or sets setup managed identity Id.
        /// </summary>
        [JsonProperty(
            PropertyName = "type",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets setup managed identity Id.
        /// </summary>
        [JsonProperty(
            PropertyName = "tenantId",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets setup managed identity Id.
        /// </summary>
        [JsonProperty(
            PropertyName = "principalId",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets the user assigned identities.
        /// </summary>
        [JsonProperty(
            PropertyName = "userAssignedIdentities",
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, UserAssignedIdentityProperties> UserAssignedIdentities { get; set; }
    }

#pragma warning restore CS8618
}
