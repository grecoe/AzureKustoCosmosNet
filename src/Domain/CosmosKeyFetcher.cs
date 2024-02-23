namespace SubscriptionCleanupUtils.Domain
{
    using Azure.Core;
    using Newtonsoft.Json;
    using SubscriptionCleanupUtils.Domain.Interface;
    using System.Net.Http.Headers;

    /// <summary>
    /// Result of calling the listKeys API
    /// </summary>
    internal class DatabaseAccountListKeysResult
    {
        public string PrimaryMasterKey { get; set; } = string.Empty;
        public string PrimaryReadonlyMasterKey { get; set; } = string.Empty;
        public string SecondaryMasterKey { get; set; } = string.Empty;
        public string SecondaryReadonlyMasterKey { get; set; } = string.Empty;
    }


    /// <summary>
    /// Utility for acquiring the service keys to a Comsos instance so that the instance
    /// can be contacted.
    /// 
    /// Whatever credentials are used, they must have the ability to list keys on the instance
    /// or this will not work. 
    /// 
    /// Would be handy to add in some logging here. 
    /// </summary>
    internal class CosmosKeyFetcher
    {
        public const string AzureResourceManagerScope = "https://management.azure.com";
        private const string RequestParameterListKeys = "/listKeys?api-version={0}";
        private const string DefaultCosmosListKeysVersion = "2019-12-12";

        private const string Scheme = "Bearer";




        public static async Task<string> GetCosmosDbPrimaryMasterKeyAsync(
            ITokenProvider tokenProvider,
            string cosmosDbResourceId, 
            string? listKeyVersion = null)
        {
            var endpoint = GetKeysRequestUri(
                cosmosDbResourceId,
                string.Format(CosmosKeyFetcher.RequestParameterListKeys,
                    listKeyVersion ?? CosmosKeyFetcher.DefaultCosmosListKeysVersion));

            string payload = await GetPayload(tokenProvider, endpoint, "Cosmos Primary Key");

            var databaseAccountListKeyResult =
                JsonConvert.DeserializeObject<DatabaseAccountListKeysResult>(payload);

            return databaseAccountListKeyResult != null ? databaseAccountListKeyResult.PrimaryMasterKey : string.Empty;
        }

        private static string GetKeysRequestUri(string resourceId, string version)
        {
            var slashDelimiter = resourceId.StartsWith("/", StringComparison.Ordinal) ? string.Empty : "/";
            return $"{CosmosKeyFetcher.AzureResourceManagerScope}{slashDelimiter}{resourceId}{version}";
        }

        private static async Task<string> GetPayload(ITokenProvider tokenProvider, string endpoint, string keyType)
        {
            string accessToken = string.Empty;
            try
            {
                accessToken = GetAccessToken(tokenProvider);
            }
            catch (Exception ex)
            {
                throw;
            }

            string payload = string.Empty;
            try
            {
                payload = await GetRequestBodyAsString(endpoint, accessToken);
            }
            catch (Exception ex)
            {
                throw;
            }

            return payload;
        }

        private static string GetAccessToken(ITokenProvider tokenProvider)
        {
            TokenCredential azureServiceTokenProvider = tokenProvider.Credential; //new DefaultAzureCredential();

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken cancellationToken = source.Token;
            TokenRequestContext context = new TokenRequestContext(new string[] { CosmosKeyFetcher.AzureResourceManagerScope });
            AccessToken token = azureServiceTokenProvider.GetToken(context, cancellationToken);

            return token.Token;
        }

        private static async Task<string> GetRequestBodyAsString(string endpoint, string accessToken)
        {
            using (var httpClient = new HttpClient())
            using (var postContent = new StringContent(string.Empty))
            using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint)))
            {
                request.Content = postContent;
                request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, accessToken);

                var result = await httpClient.SendAsync(request);

                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}
