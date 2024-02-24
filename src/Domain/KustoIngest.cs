//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.Core;
    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Ingest;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using System.Text;

    internal class KustoIngest : IDisposable
    {
        private readonly TokenCredential Credential;
        private readonly KustoConnectionStringBuilder KustoCSB;
        private bool Dispposed { get; set; } = false;
        private EventLogSettings EventLogSettings { get; set; }
        private IKustoIngestClient KustoIngestClient { get; set; }
        private KustoIngestionProperties IngestProperties { get; set; }

        public KustoIngest(
            TokenCredential tokenCredential,
            EventLogSettings eventLogSettings
            )
        {
            this.EventLogSettings = eventLogSettings;
            this.Credential = tokenCredential;

            this.KustoCSB = new KustoConnectionStringBuilder(
                this.EventLogSettings.IngestEndpoint,
                this.EventLogSettings.Database)
                .WithAadAzureTokenCredentialsAuthentication(this.Credential);

            this.KustoIngestClient = KustoIngestFactory.CreateStreamingIngestClient(this.KustoCSB);
            this.IngestProperties = new KustoIngestionProperties(
                this.EventLogSettings.Database,
                this.EventLogSettings.Table)
            {
                Format = DataSourceFormat.json,
                IngestionMapping = new IngestionMapping
                {
                    IngestionMappingKind = Kusto.Data.Ingestion.IngestionMappingKind.Json
                }
            };
        }

        public void StreamRecord(IKustoRecord record)
        {
            using (Stream inputStream = CreateLogStream(record))
            {
                using (var ingestClient = KustoIngestFactory.CreateStreamingIngestClient(this.KustoCSB))
                {
                    this.KustoIngestClient.IngestFromStream(inputStream, this.IngestProperties);
                }
            }
        }

        private static Stream CreateLogStream(IKustoRecord record)
        {
            var ms = new MemoryStream();
            using (var tw = new StreamWriter(ms, Encoding.UTF8, 4096, true))
            {
                tw.WriteLine(record.GetEntity());
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }


        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.Dispposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.KustoIngestClient != null)
                {
                    this.KustoIngestClient.Dispose();
                    this.KustoIngestClient = null;
                }
            }

            this.Dispposed = true;
        }

    }
}
