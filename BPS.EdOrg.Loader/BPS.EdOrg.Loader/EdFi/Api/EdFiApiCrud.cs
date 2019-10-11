﻿using System.Configuration;
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
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            return client.Execute(request);

        }

        /// <summary>
        /// Update the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        public IRestResponse PutData(string jsonData, RestClient client, string token)
        {
            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            return client.Execute(request);

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
            return client.Execute(request);
        }
    }
}
