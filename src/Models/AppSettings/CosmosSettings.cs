namespace SubscriptionCleanupUtils.Models.AppSettings
{
    internal class  CosmosDetail
    {
        /// <summary>
        /// Each product environment (DOGFOOD, STAGING, CANARY, PROD) all have 
        /// a distinct Cosmos DB instance. This property is used to identify the
        /// environemnt this isntance supports.
        /// </summary>
        public string Environment { get; set; } = string.Empty;
        /// <summary>
        /// Friendly name, not used in processing
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// https://... endpoint of the Comsos
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Azure Resource ID of the Cosmos.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// The database to attach to. Internally, callers must figure out what tables
        /// they want to read from. 
        /// </summary>
        public string CosmosDatabase { get; set; } = string.Empty;

    }
    internal class CosmosSettings
    {
        public const string SECTION = "CosmosSettings";
        /// <summary>
        /// The environments suppported. As new ones are added, this list should be updated.
        /// </summary>
        public List<string> AcceptableInstanceEnvironment { get; set; } = new List<string>();
        public List<CosmosDetail> Environments { get; set; } = new List<CosmosDetail>();

    }
}
