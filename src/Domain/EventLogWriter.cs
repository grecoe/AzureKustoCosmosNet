//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    /*
Portal -> Configuration -> Streaming ingestion : enable

.create table CleanEventLog ( Timestamp:datetime, CorrelationId:string, Level:string, Service:string, Subscription:string, Message:string ,Data:string)

.alter table CleanEventLog policy streamingingestion enable

.drop table CleanEventLog

 // Example queries
CleanEventLog
| order by Timestamp desc
| extend Content= parse_json(Data)
| project-away  Data
| where CorrelationId == "81ce7f04-83fa-4b2d-9435-2584f84d7e76"
| where Message contains "Delete resource Group"
// Where did cleanup happen
| summarize count() by Subscription

// To see what all have instances in this run
| where Message contains "Total ADME Instances"


     */
    using Azure.Core;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using SubscriptionCleanupUtils.Models.Kusto;

    internal class EventLogWriter : KustoIngest, IEventLogger
    {
        #region Private
        private string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        #endregion

        public string Subscription { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;

        public EventLogWriter(
            TokenCredential tokenCredential,
            EventLogSettings eventLogSettings
            )
            : base(tokenCredential, new KustoIngestSettings(eventLogSettings))
        {
        }

        public void LogInfo(string message, object? payload = null)
        {
            this.LogEvent("INFO", this.Subscription, this.Service, message, payload);
        }

        public void LogWarning(string message, object? payload = null)
        {
            this.LogEvent("WARN", this.Subscription, this.Service, message, payload);
        }

        public void LogError(string message, object? payload = null)
        {
            this.LogEvent("ERROR", this.Subscription, this.Service, message, payload);
        }

        public void LogException(string message, Exception exception)
        {
            Dictionary<string, string> prop = new Dictionary<string, string>();
            prop.Add("Message", exception.Message);
            if(exception.InnerException != null)
            {
                prop.Add("Inner", exception.InnerException.Message);
            }
            this.LogEvent("Exception", this.Subscription, this.Service, message, prop);
        }

        public void LogEvent(string level, string subscription, string service, string message, object? payload = null)
        {
            EventLogRecordDTO record = new EventLogRecordDTO();
            record.Timestamp = DateTime.UtcNow;
            record.Level = level;
            record.CorrelationId = this.CorrelationId;
            record.Service = service;
            record.Subscription = subscription;
            record.Message = message;
            record.Data = string.Empty;

            if(payload != null)
            {
                if (payload is string)
                {
                    record.Data = payload.ToString() ?? "";
                }
                else
                {
                    record.Data = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);
                }

            }

            this.StreamRecords(new List<IKustoRecord>() { record });
        }
    }
}
