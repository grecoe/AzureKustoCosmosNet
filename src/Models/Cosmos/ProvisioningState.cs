namespace SubscriptionCleanupUtils.Models.Cosmos
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Provisioning state of tracked resource.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProvisioningState
    {
        Unknown,

        Succeeded,

        Failed,

        Canceled,

        Creating,

        Deleting,

        Updating,

        UpgradingDPVersion,

        Deleted,

        FailedUpgradingDPVersion
    }
}
