namespace SubscriptionCleanupUtils.Models.Cosmos
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverageAttribute]
    public class EdsProperties
    {
        public EdsProperties(string keyvaultUrl, bool isEdsEnabled)
        {
            this.KeyvaultUrl = keyvaultUrl;
            this.IsEdsEnabled = isEdsEnabled;
        }

        public EdsProperties() { }

        /// <summary>
        /// Gets or sets customers keyvault url .
        /// </summary>
        [JsonProperty(PropertyName = "keyvaultUrl")]
        public string KeyvaultUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the boolean flag for isEdsEnabled .
        /// </summary >
        [JsonProperty(PropertyName = "isEdsEnabled")]
        public bool IsEdsEnabled { get; set; }
    }
}
