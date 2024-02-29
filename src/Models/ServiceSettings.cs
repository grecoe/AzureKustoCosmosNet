//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Models.AppSettings;

    /// <summary>
    /// A collection of the items from the appsettings.json that are used across 
    /// multiple services. Self explanitory what each one does. 
    /// </summary>
    internal class ServiceSettings
    {
        public KustoSettings? KustoSettings { get; private set; }
        public ServiceTreeSettings? ServiceTreeSettings { get; private set; }
        public ExecutionSettings? ExecutionSettings { get; private set; }
        public DNSSettings? DNSSettings { get; private set; }
        public EventLogSettings? EventLogSettings { get; private set; }

        public CosmosSettings? CosmosSettings { get; private set; }

        public ServiceSettings(IConfiguration configuration)
        {
            this.CosmosSettings = configuration.GetSection(CosmosSettings.SECTION).Get<CosmosSettings>();
            if(this.CosmosSettings == null)
            {
                throw new ArgumentNullException(nameof(CosmosSettings));
            }

            this.KustoSettings = configuration.GetSection(KustoSettings.SECTION).Get<KustoSettings>();
            if (this.KustoSettings == null)
            {
                throw new ArgumentNullException(nameof(this.KustoSettings));
            }

            this.ServiceTreeSettings = configuration.GetSection(ServiceTreeSettings.SECTION).Get<ServiceTreeSettings>();
            if (this.ServiceTreeSettings == null)
            {
                throw new ArgumentNullException(nameof(this.ServiceTreeSettings));
            }

            this.ExecutionSettings = configuration.GetSection(ExecutionSettings.SECTION).Get<ExecutionSettings>();
            if (this.ExecutionSettings == null)
            {
                throw new ArgumentNullException(nameof(this.ExecutionSettings));
            }

            this.DNSSettings = configuration.GetSection(DNSSettings.SECTION).Get<DNSSettings>();
            if (this.DNSSettings == null)
            {
                throw new ArgumentNullException(nameof(this.DNSSettings));
            }

            this.EventLogSettings = configuration.GetSection(EventLogSettings.SECTION).Get<EventLogSettings>();
            if (this.EventLogSettings == null)
            {
                throw new ArgumentNullException(nameof(this.EventLogSettings));
            }
        }
    }
}
