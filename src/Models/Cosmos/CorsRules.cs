namespace SubscriptionCleanupUtils.Models.Cosmos
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// CorsRule class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CorsRules
    {
        public CorsRules()
        {
            // parameterless constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsRules"/> class.
        /// </summary>
        /// <param name="allowedHeaders">allowedHeaders.</param>
        /// <param name="allowedMethods">allowedMethods.</param>
        /// <param name="allowedOrigins">allowedOrigins.</param>
        /// <param name="exposedHeaders">exposedHeaders.</param>
        /// <param name="maxAgeInSeconds">maxAgeInSeconds.</param>
        public CorsRules(
            List<string> allowedOrigins,
            List<string> allowedMethods,
            List<string> allowedHeaders,
            List<string> exposedHeaders,
            int maxAgeInSeconds)
        {
            this.AllowedOrigins = allowedOrigins;
            this.AllowedMethods = allowedMethods;
            this.AllowedHeaders = allowedHeaders;
            this.ExposedHeaders = exposedHeaders;
            this.MaxAgeInSeconds = maxAgeInSeconds;
        }

        /// <summary>
        /// Gets or sets list of allowed HTTP methods.
        /// </summary>
        [JsonProperty(PropertyName = "allowedMethods")]
        public List<string> AllowedMethods { get; set; }

        /// <summary>
        /// Gets or sets list of allowed origin domains
        /// </summary>
        [JsonProperty(PropertyName = "allowedOrigins")]
        public List<string> AllowedOrigins { get; set; }

        /// <summary>
        /// Gets or sets list of allowed request headers
        /// </summary>
        [JsonProperty(PropertyName = "allowedHeaders")]
        public List<string> AllowedHeaders { get; set; }

        /// <summary>
        /// Gets or sets list of exposed response headers
        /// </summary>
        [JsonProperty(PropertyName = "exposedHeaders")]
        public List<string> ExposedHeaders { get; set; }

        /// <summary>
        /// Gets or sets max age in seconds.
        /// </summary>
        [JsonProperty(PropertyName = "maxAgeInSeconds")]
        public int? MaxAgeInSeconds { get; set; }
    }
}
