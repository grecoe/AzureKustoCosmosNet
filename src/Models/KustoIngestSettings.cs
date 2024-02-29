namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Models.AppSettings;

    internal class KustoIngestSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;

        public KustoIngestSettings(string endpoint, string database, string table)
        {
            this.Endpoint = endpoint;
            this.Database = database;
            this.Table = table;
        }

        public KustoIngestSettings(EventLogSettings settings)
        {
            this.Endpoint = settings.IngestEndpoint;
            this.Database = settings.Database;
            this.Table = settings.Table;
        }
    }
}
