using System;
using System.Text;


namespace BPS.EdPlanLoaderCore.MetaData
{
    public class EdorgConfiguration
    {
               
        public string ApiUrl { get; set; }                
        public string OauthUrl { get; set; }    
        public string DataFilePathEdPlantoApen { get; set; }
        public string DataFilePathSpedSims { get; set; }  
        public bool Metadata { get; set; }        
        public string XsdFolder { get; set; }
        public string ApiLoaderExePath { get; set; }
        internal string Token { get; set; }

        /// <summary>
        /// Gets a string containing error messages for any missing or invalid configuration options.
        /// Each line describes a specific option that failed validation.
        /// </summary>
        public string ErrorText
        {
            get
            {
                var sb = new StringBuilder();               

                // Check if ApiUrl is missing or is not a well-formed absolute URL
                if (string.IsNullOrEmpty(ApiUrl) || !Uri.IsWellFormedUriString(ApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'a:apiurl' parse error. Provided value is not a url.");                

                // Check if OauthUrl is missing or is not a well-formed absolute URL
                if (string.IsNullOrEmpty(OauthUrl) || !Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'o:oauthurl' parse error. Provided value is not a url.");                

                // Return the concatenated error messages
                return sb.ToString();
            }
        }
    }
}
