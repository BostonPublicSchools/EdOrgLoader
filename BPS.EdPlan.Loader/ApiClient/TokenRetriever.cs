using RestSharp;
using System.Net;
using System.Security.Authentication;

namespace EdPlanLoaderCore.ApiClient
{
    public interface ITokenRetriever
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string ObtainNewBearerToken();
    }

    public class TokenRetriever : ITokenRetriever
    {
        private string oauthUrl;
        private string clientKey;
        private string clientSecret;

        // <summary>
        /// Initializes a new instance of the <see cref="TokenRetriever"/> class with the specified OAuth URL, client key, and client secret.
        /// </summary>
        /// <param name="oauthUrl">The OAuth endpoint URL used to retrieve tokens.</param>
        /// <param name="clientKey">The client key for OAuth authentication.</param>
        /// <param name="clientSecret">The client secret for OAuth authentication.</param>
        public TokenRetriever(string oauthUrl, string clientKey, string clientSecret)
        {
            this.oauthUrl = oauthUrl;
            this.clientKey = clientKey;
            this.clientSecret = clientSecret;
        }

        // <summary>
        /// Creates a new RestClient using the configured OAuth URL and obtains a new Bearer token.
        /// </summary>
        /// <returns>The Bearer token as a string.</returns>
        public string ObtainNewBearerToken()
        {
            var oauthClient = new RestClient(oauthUrl);
            //var accessCode = GetAccessCode(oauthClient);
            return GetBearerToken(oauthClient);
        }

        /// <summary>
        /// Retrieves an OAuth access code by sending an authorization request to the OAuth server.
        /// </summary>
        /// <param name="oauthClient">The OAuth REST client used to send the request.</param>
        /// <returns>The access code as a string.</returns>
        /// <exception cref="AuthenticationException">
        /// Thrown if the authorization code cannot be retrieved due to a non-OK status code or an error in the response.
        /// </exception>
        private string GetAccessCode(IRestClient oauthClient)
        {
            // Create a new POST request to the "oauth/authorize" endpoint
            var accessCodeRequest = new RestRequest("oauth/authorize", Method.POST);

            // Add required parameters for the OAuth authorization request
            accessCodeRequest.AddParameter("Client_id", clientKey);
            accessCodeRequest.AddParameter("Response_type", "code");

            // Execute the request and obtain the response, deserializing into AccessCodeResponse
            var accessCodeResponse = oauthClient.Execute<AccessCodeResponse>(accessCodeRequest);

            // Throw an exception if the response status is not OK
            if (accessCodeResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException("Unable to retrieve an authorization code. Error message: " +
                                                  accessCodeResponse.ErrorMessage);
            }

            // Throw an exception if the response contains an error
            if (accessCodeResponse.Data.Error != null)
            {
                throw new AuthenticationException(
                    "Unable to retrieve an authorization code. Please verify that your application key is correct. Alternately, the service address may not be correct: " +
                    oauthUrl);
            }
            // Return the access code from the response data
            return accessCodeResponse.Data.Code;
        }

        // <summary>
        /// Retrieves a bearer (access) token from the OAuth server using client credentials.
        /// </summary>
        /// <param name="oauthClient">The OAuth REST client used to send the token request.</param>
        /// <returns>The bearer access token as a string.</returns>
        /// <exception cref="AuthenticationException">
        /// Thrown if the access token cannot be retrieved due to a non-OK status code or an error in the response.
        /// </exception>
        private string GetBearerToken(IRestClient oauthClient)
        {
            var bearerTokenRequest = new RestRequest("oauth/token", Method.POST);
            bearerTokenRequest.AddParameter("Client_id", clientKey);
            bearerTokenRequest.AddParameter("Client_secret", clientSecret);
            bearerTokenRequest.AddParameter("Grant_type", "client_credentials");
            // version 2.5
            //bearerTokenRequest.AddParameter("Code", accessCode);
            //bearerTokenRequest.AddParameter("Grant_type", "authorization_code");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var bearerTokenResponse = oauthClient.Execute<BearerTokenResponse>(bearerTokenRequest);
            if (bearerTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException("Unable to retrieve an access token. Error message: " +
                                                  bearerTokenResponse.ErrorMessage);
            }

            if (bearerTokenResponse.Data.Error != null || bearerTokenResponse.Data.Token_type != "bearer")
            {
                throw new AuthenticationException(
                    "Unable to retrieve an access token. Please verify that your application secret is correct.");
            }

            return bearerTokenResponse.Data.Access_token;
        }
    }

    // <summary>
    /// Represents the response from the OAuth authorization endpoint when requesting an access code.
    /// </summary>
    internal class AccessCodeResponse
    {
        public string Code { get; set; }
        public string State { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Represents the response from the OAuth token endpoint when requesting a bearer (access) token.
    /// </summary>
    internal class BearerTokenResponse
    {
        public string Access_token { get; set; }
        public string Expires_in { get; set; }
        public string Token_type { get; set; }
        public string Error { get; set; }
    }

}
