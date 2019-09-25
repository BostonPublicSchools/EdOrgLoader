using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    public class Configuration
    {
        public string CrossWalkOAuthUrl { get; set; }
        public string CrossWalkSchoolApiUrl { get; set; }
        public string CrossWalkStaffApiUrl { get; set; }
        public string CrossWalkServiceCenterApiUrl { get; set; }
        public string CrossWalkKey { get; set; }
        public string CrossWalkSecret { get; set; }
        public string ApiUrl { get; set; }
        public int SchoolYear { get; set; }
        public string OauthKey { get; set; }
        public string OauthSecret { get; set; }
        public string OauthUrl { get; set; }
        public string XMLOutputPath { get; set; }
        public string DataFilePath { get; set; }
        public string DataFilePathJob { get; set; }
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
                        string.IsNullOrEmpty(DataFilePath) || 
                        string.IsNullOrEmpty(DataFilePathJob) ||
                        string.IsNullOrEmpty(CrossWalkFilePath) ||
                        string.IsNullOrEmpty(XsdFolder) ||
                        string.IsNullOrEmpty(InterchangeOrderFolder) ||
                        string.IsNullOrEmpty(MetadataUrl) ||
                        string.IsNullOrEmpty(OauthKey) ||
                        string.IsNullOrEmpty(OauthSecret) ||
                        string.IsNullOrEmpty(OauthUrl) ||
                        string.IsNullOrEmpty(CrossWalkOAuthUrl) ||
                        string.IsNullOrEmpty(CrossWalkSchoolApiUrl) ||
                        string.IsNullOrEmpty(CrossWalkStaffApiUrl) ||
                        string.IsNullOrEmpty(CrossWalkServiceCenterApiUrl) ||
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

                if (string.IsNullOrEmpty(CrossWalkStaffApiUrl) || !Uri.IsWellFormedUriString(CrossWalkStaffApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'p:CrossWalkStaffApiUrl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(CrossWalkServiceCenterApiUrl) || !Uri.IsWellFormedUriString(CrossWalkServiceCenterApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'p:CrossWalkServiceCenterApiUrl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(XMLOutputPath) || !Directory.Exists(XMLOutputPath))
                    sb.AppendLine("Option 'd:data' parse error. Provided value is not a directory.");

                if (string.IsNullOrEmpty(DataFilePath) || !Directory.Exists(DataFilePath))
                    sb.AppendLine("Option 'b:data' parse error. Provided value is not a file path.");

                if (string.IsNullOrEmpty(DataFilePathJob) || !Directory.Exists(DataFilePathJob))
                    sb.AppendLine("Option 'j:data' parse error. Provided value is not a file path.");

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
