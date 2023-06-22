using System.Configuration;
using BPS.EdOrg.Loader.ApiClient;
using RestSharp;

namespace BPS.EdOrg.Loader.EdFi.Api
{
    class EdFiApiCrud
    {
        public string GetAuthToken()
        {
            var tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);
            var token = tokenRetriever.ObtainNewBearerToken();
            return token;
        }

        /// <summary>
        /// POST the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse PostData(string jsonData, RestClient client, string token)
        {
            if (token == null) token = GetAuthToken();
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if ((int)response.StatusCode == 401)
            {
                token = GetAuthToken();
                return PostData(jsonData, client, token);
            }
            return response;
           

        }

        /// <summary>
        /// Update the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse PutData(string jsonData, RestClient client, string token)
        {
            if (token == null) token = GetAuthToken();
            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if ((int)response.StatusCode == 401)
            {
                token = GetAuthToken();
                return PutData(jsonData,client, token);
            }
            return response;

        }

        /// <summary>
        /// Patch update the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse PatchUpdateData(string jsonData, RestClient client, string token)
        {
            if (token == null) token = GetAuthToken();
            var request = new RestRequest(Method.PATCH);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if ((int)response.StatusCode == 401)
            {
                token = GetAuthToken();
                return PutData(jsonData, client, token);
            }
            return response;

        }

        /// <summary>
        /// Gets the Data from the ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse GetData(RestClient client, string token)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if ((int)response.StatusCode == 401)
            {
                token = GetAuthToken();
                return GetData(client, token);
            }
                
            return response;


        }

        /// <summary>
        /// Delete the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse DeleteData(RestClient client, string token)
        {
            if (token == null) token = GetAuthToken();
            var request = new RestRequest(Method.DELETE);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if ((int)response.StatusCode == 401)
            {
                token = GetAuthToken();
                return DeleteData(client, token);
            }
            return response;

        }
    }
}
