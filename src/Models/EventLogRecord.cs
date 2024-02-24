//
// Copyright (c) 2024 Microsoft 
//
using SubscriptionCleanupUtils.Domain.Interface;

namespace SubscriptionCleanupUtils.Models
{
    internal class EventLogRecord : IKustoRecord
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string CorrelationId { get; set; }
        public string Service { get; set; }
        public string Subscription { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }

        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }

}
