//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.Kusto
{
    using SubscriptionCleanupUtils.Domain.Interface;

    internal class EventLogRecordDTO : IKustoRecord
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Subscription { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }

}
