using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionCleanupUtils.Models.AppSettings
{
    internal class EventLogSettings
    {
        public const string SECTION = "EventLog";

        public string IngestEndpoint { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
    }
}
