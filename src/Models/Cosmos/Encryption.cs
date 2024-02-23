namespace SubscriptionCleanupUtils.Models.Cosmos
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverageAttribute]
    public class KeyVaultProperties
    {
        /// <summary>
        /// Initializes a new instance of the KeyVaultProperties class.
        /// </summary>
        public KeyVaultProperties() { }

        /// <summary>
        /// Initializes a new instance of the KeyVaultProperties class.
        /// </summary>
        /// <param name="keyName">The name of the key vault key.</param>
        /// <param name="keyVersion">The version of the key vault key.</param>
        /// <param name="keyVaultUri">The Uri of the key vault.</param>
        /// <param name="userIdentity">The user assigned identity (ARM resource
        /// id) that has access to the key.</param>
        public KeyVaultProperties(string keyName = default(string), string keyVersion = default(string), string keyVaultUri = default(string), string userIdentity = default(string))
        {
            this.KeyName = keyName;
            this.KeyVersion = keyVersion;
            this.KeyVaultUri = keyVaultUri;
            this.UserIdentity = userIdentity;
        }

        /// <summary>
        /// Gets or sets the name of the key vault key.
        /// </summary>
        [JsonProperty(PropertyName = "keyName")]
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the version of the key vault key.
        /// </summary>
        [JsonProperty(PropertyName = "keyVersion")]
        public string KeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the Uri of the key vault.
        /// </summary>
        [JsonProperty(PropertyName = "keyVaultUri")]
        public string KeyVaultUri { get; set; }

        /// <summary>
        /// Gets or sets the user assigned identity (ARM resource id) that has
        /// access to the key.
        /// </summary>
        [JsonProperty(PropertyName = "userIdentity")]
        public string UserIdentity { get; set; }
    }

    [ExcludeFromCodeCoverageAttribute]
    public class Encryption
    {
        /// <summary>
        /// Initializes a new instance of the Encryption class.
        /// </summary>
        public Encryption() { }

        /// <summary>
        /// Initializes a new instance of the Encryption class.
        /// </summary>
        /// <param name="keySource">The encryption keySource (provider).
        /// Possible values (case-insensitive):  Microsoft.Storage,
        /// Microsoft.Keyvault. Possible values include: 'Microsoft.Storage',
        /// 'Microsoft.Keyvault'</param>
        /// <param name="keyVaultProperties">Properties provided by key
        public Encryption(string keySource, KeyVaultProperties keyVaultProperties = default(KeyVaultProperties))
        {
            this.KeySource = keySource;
            this.KeyVaultProperties = keyVaultProperties;
        }

        /// <summary>
        /// Gets or sets properties provided by key vault.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "keyVaultProperties")]
        public KeyVaultProperties KeyVaultProperties { get; set; }

        /// <summary>
        /// Gets or sets the encryption keySource (provider). Possible values
        /// (case-insensitive):  Microsoft.Storage, Microsoft.Keyvault.
        /// Possible values include: 'Microsoft.Storage', 'Microsoft.Keyvault'
        /// </summary>
        [JsonProperty(PropertyName = "keySource")]
        public string KeySource { get; set; }
    }
}
