using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
namespace BPS.EdOrg.Loader.MetaData
{
    public class EdorgConfiguration
    {
        public string CrossWalkOAuthUrl { get; set; }        
        public string CrossWalkSchoolApiUrl { get; set; }
        public string CrossWalkEdServicecenterApiUrl { get; set; }
        public string CrossWalkStaffApiUrl { get; set; }
        public string CrossWalkKey { get; set; }
        public string CrossWalkSecret { get; set; }
        public string ApiUrl { get; set; }
        public int SchoolYear { get; set; }
        public string OauthKey { get; set; }
        public string OauthSecret { get; set; }
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
        public string CrossWalkFilePath { get; set; }
        public string WorkingFolder { get; set; }
        public string Profile { get; set; }
        public bool Metadata { get; set; }
        public string MetadataUrl { get; set; }
        public string XsdFolder { get; set; }
        public string InterchangeOrderFolder { get; set; }
        public string ApiLoaderExePath { get; set; }
        internal string Token { get; set; }
        
        public bool IsValid
        {
            get
            {
                var result =
                    !(
                        string.IsNullOrEmpty(ApiUrl) ||
                        string.IsNullOrEmpty(WorkingFolder) ||
                        string.IsNullOrEmpty(XMLOutputPath) ||
                        string.IsNullOrEmpty(JobFilePath) ||
                        string.IsNullOrEmpty(DataFilePath) || 
                        string.IsNullOrEmpty(DataFilePathJob) ||
                        string.IsNullOrEmpty(DataFilePathStaffEmail) ||
                        string.IsNullOrEmpty(DataFilePathStaffAddressEmployees) ||
                        string.IsNullOrEmpty(DataFilePathStaffAddressA) ||
                        string.IsNullOrEmpty(DataFilePathStaffAddressB) ||
                        string.IsNullOrEmpty(DataFilePathJobTransfer) ||
                        string.IsNullOrEmpty(DataFilePathStaffPhoneNumbers) ||
                        string.IsNullOrEmpty(DataFilePathEdPlantoApen) ||
                        string.IsNullOrEmpty(DataFilePathSpedSims) ||
                        string.IsNullOrEmpty(CrossWalkFilePath) ||
                        string.IsNullOrEmpty(XsdFolder) ||
                        string.IsNullOrEmpty(InterchangeOrderFolder) ||
                        string.IsNullOrEmpty(MetadataUrl) ||
                        string.IsNullOrEmpty(OauthKey) ||
                        string.IsNullOrEmpty(OauthSecret) ||
                        string.IsNullOrEmpty(OauthUrl) ||
                        string.IsNullOrEmpty(CrossWalkOAuthUrl) ||
                        string.IsNullOrEmpty(CrossWalkSchoolApiUrl) ||
                        string.IsNullOrEmpty(CrossWalkEdServicecenterApiUrl) ||
                        string.IsNullOrEmpty(CrossWalkStaffApiUrl) ||
                        string.IsNullOrEmpty(CrossWalkKey) ||
                        string.IsNullOrEmpty(CrossWalkSecret)
                    )
                    && Directory.Exists(XMLOutputPath)
                    && Uri.IsWellFormedUriString(ApiUrl, UriKind.Absolute)
                    && Uri.IsWellFormedUriString(MetadataUrl, UriKind.Absolute)
                    && Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute);

                return result;
            }
        }

        public string ErrorText
        {
            get
            {
                var sb = new StringBuilder();

                if (string.IsNullOrEmpty(OauthKey))
                    sb.AppendLine("Option 'k:key' parse error. missing value.");

                if (string.IsNullOrEmpty(OauthSecret))
                    sb.AppendLine("Option 's:secret' parse error. missing value.");

                if (string.IsNullOrEmpty(ApiUrl) || !Uri.IsWellFormedUriString(ApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'a:apiurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(CrossWalkOAuthUrl) || !Uri.IsWellFormedUriString(CrossWalkOAuthUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'u:crosswalkoauthurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(CrossWalkSchoolApiUrl) || !Uri.IsWellFormedUriString(CrossWalkSchoolApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'p:corsswalkapiurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(CrossWalkEdServicecenterApiUrl) || !Uri.IsWellFormedUriString(CrossWalkEdServicecenterApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option '4:CrossWalkEdServicecenterApiUrl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(CrossWalkStaffApiUrl) || !Uri.IsWellFormedUriString(CrossWalkStaffApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'p:corsswalkapiurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(XMLOutputPath) || !Directory.Exists(XMLOutputPath))
                    sb.AppendLine("Option 'd:data' parse error. Provided value is not a directory.");

                if (string.IsNullOrEmpty(JobFilePath) || !Directory.Exists(JobFilePath))
                    sb.AppendLine("Option 'u:dataPath' parse error. Provided value is not a directory.");

                if (string.IsNullOrEmpty(DataFilePath) || !Directory.Exists(DataFilePath))
                    sb.AppendLine("Option 'b:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathJob) || !Directory.Exists(DataFilePathJob))
                    sb.AppendLine("Option 'j:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathStaffPhoneNumbers) || !Directory.Exists(DataFilePathStaffPhoneNumbers))
                    sb.AppendLine("Option 'g:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathJobTransfer) || !Directory.Exists(DataFilePathJobTransfer))
                    sb.AppendLine("Option 'i:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathStaffEmail) || !Directory.Exists(DataFilePathStaffEmail))
                    sb.AppendLine("Option 'f:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathStaffAddressEmployees) || !Directory.Exists(DataFilePathStaffAddressEmployees))
                    sb.AppendLine("Option 'h:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathStaffAddressA) || !Directory.Exists(DataFilePathStaffAddressA))
                    sb.AppendLine("Option '1:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathStaffAddressB) || !Directory.Exists(DataFilePathStaffAddressB))
                    sb.AppendLine("Option '2:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathEdPlantoApen) || !Directory.Exists(DataFilePathEdPlantoApen))
                    sb.AppendLine("Option '3:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathSpedSims) || !Directory.Exists(DataFilePathSpedSims))
                    sb.AppendLine("Option '4:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(CrossWalkFilePath) || !Directory.Exists(CrossWalkFilePath))
                    sb.AppendLine("Option 'c:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(MetadataUrl) || !Uri.IsWellFormedUriString(MetadataUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'metadataurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(OauthUrl) || !Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'o:oauthurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(WorkingFolder) || !Directory.Exists(WorkingFolder))
                    sb.AppendLine("Option 'w:working' parse error. Provided value is not a directory.");

                if (string.IsNullOrEmpty(InterchangeOrderFolder) || !Directory.Exists(InterchangeOrderFolder))
                    sb.AppendLine("Option 'i:Interchange' parse error. Provided value is not a directory.");

                return sb.ToString();
            }
        }
    }
}
