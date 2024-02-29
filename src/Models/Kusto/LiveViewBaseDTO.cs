namespace SubscriptionCleanupUtils.Models.Kusto
{
    internal class LiveViewBaseDTO
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Subscription { get; set; } = string.Empty;
    }

    internal class LiveViewInstanceBaseDTO : LiveViewBaseDTO
    {
        public string Instance { get; set; } = string.Empty;
    }
}
