using System;
using System.IO;
using System.Linq;
using System.Text;


namespace BPS.EdPlanLoaderCore.MetaData
{
    public class EdorgConfiguration
    {
        public string CrossWalkOAuthUrl { get; set; }
        public string CrossWalkSchoolApiUrl { get; set; }
        public string ApiUrl { get; set; }
        public int SchoolYear { get; set; }
        
        public string OauthUrl { get; set; }
        public string XMLOutputPath { get; set; }
        public string JobFilePath { get; set; }
        public string DataFilePath { get; set; }
        public string DataFilePathStaffEmail { get; set; }
        public string DataFilePathStaffAddressEmployees { get; set; }
        public string DataFilePathStaffAddressA { get; set; }
        public string DataFilePathStaffAddressB { get; set; }
        public string DataFilePathJob { get; set; }
        public string DataFilePathJobPreviousFile { get; set; }
        public string DataFilePathJobTransfer { get; set; }
        public string DataFilePathStaffPhoneNumbers { get; set; }
        public string DataFilePathEdPlantoApen { get; set; }
        public string DataFilePathSpedSims { get; set; }        
        public string WorkingFolder { get; set; }
        public string Profile { get; set; }
        public bool Metadata { get; set; }
        public string MetadataUrl { get; set; }
        public string XsdFolder { get; set; }
        public string InterchangeOrderFolder { get; set; }
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
                if (string.IsNullOrEmpty(CrossWalkSchoolApiUrl) || !Uri.IsWellFormedUriString(CrossWalkSchoolApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'p:corsswalkapiurl' parse error. Provided value is not a url.");

                // Check if OauthUrl is missing or is not a well-formed absolute URL
                if (string.IsNullOrEmpty(OauthUrl) || !Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'o:oauthurl' parse error. Provided value is not a url.");

                // Check if InterchangeOrderFolder is missing or points to a non-existent directory
                if (string.IsNullOrEmpty(InterchangeOrderFolder) || !Directory.Exists(InterchangeOrderFolder))
                    sb.AppendLine("Option 'i:Interchange' parse error. Provided value is not a directory.");

                // Return the concatenated error messages
                return sb.ToString();
            }
        }
    }
}
