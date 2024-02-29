using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionCleanupUtils.Models.Cosmos
{
#pragma warning disable CS8618

    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverageAttribute]
    public class DelegatedHoboV2Properties
    {
        /// <summary>
        /// Gets or sets Source resource Azure internal id.
        /// </summary>
        [JsonProperty(PropertyName = "internal_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string InternalId { get; set; }

        /// <summary>
        /// Gets or sets Source resource Azure resource id.
        /// </summary>
        [JsonProperty(PropertyName = "resource_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets MIRP delegationRecord persistent id.
        /// </summary>
        [JsonProperty(PropertyName = "delegation_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DelegationId { get; set; }

        /// <summary>
        /// Gets or sets Delegation_URL to chain the delegation further.
        /// </summary>
        [JsonProperty(PropertyName = "delegation_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DelegationUrl { get; set; }
    }

#pragma warning restore CS8618
}
