//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.AppSettings
{
    internal class KustoSettings
    {
        public const string SECTION = "KustoSettings";

        public string Name { get; set; } = "Kusto";
        public string LiveViewEndpoint { get; set; } = string.Empty;
        public string LiveViewDatabase { get; set; } = string.Empty;
        public string ServiceTreeEndpoint { get; set; } = string.Empty;
        public string ServiceTreeDatabase { get; set; } = string.Empty;
        public string ADMEEndpoint { get; set; } = string.Empty;
        public string ADMEDatabase { get; set; } = string.Empty;
    }
}
