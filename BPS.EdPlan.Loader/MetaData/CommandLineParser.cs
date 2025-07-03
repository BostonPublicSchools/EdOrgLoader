using Fclp;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace BPS.EdPlanLoaderCore.MetaData
{
    public class CommandLineParser : FluentCommandLineParser<EdorgConfiguration>
    {
        public CommandLineParser()
        {
            Setup(arg => arg.CrossWalkSchoolApiUrl).As('p', "CrossWalkSchoolApiUrl")
                .WithDescription("The Crosswalk web API url (i.e. http://server/api/v1.0)")
                .SetDefault(ConfigurationManager.AppSettings["CrossWalkSchoolApiUrl"]);              

            Setup(arg => arg.ApiUrl).As('a', "apiurl")
                .WithDescription("The web API url (i.e. http://server/api/v1.0)")
                .SetDefault(ConfigurationManager.AppSettings["ApiUrl"]);
            
            Setup(arg => arg.SchoolYear).As('y', "year")
                .WithDescription("The target school year for the web API (i.e. 2016)")
                .SetDefault(GetFirstValue(
                        Convert.ToInt32(ConfigurationManager.AppSettings["SchoolYear"]),
                        DateTime.Today.Year));

             Setup(arg => arg.DataFilePathEdPlantoApen).As('3', "DataFilePathEdPlantoApen")
            .WithDescription("Path to folder containing the input text data files to be loaded")
            .SetDefault(GetFirstValue(
                ConfigurationManager.AppSettings["DataFilePathEdPlantoApen"],
                Directory.GetCurrentDirectory()));

            Setup(arg => arg.DataFilePathSpedSims).As('5', "DataFilePathSpedSims")
            .WithDescription("Path to folder containing the input text data files to be loaded")
            .SetDefault(GetFirstValue(
                ConfigurationManager.AppSettings["DataFilePathSpedSims"],
                Directory.GetCurrentDirectory()));           

            Setup(arg => arg.OauthUrl).As('o', "oauthurl")
                .WithDescription("The OAuth url (i.e. http://server/oauth)")
                .SetDefault(ConfigurationManager.AppSettings["OAuthUrl"]);

            Setup(arg => arg.XsdFolder).As('x', "xsd")
                .WithDescription("Path to a folder containing the Ed-Fi Xsd Schema files")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["XsdFolder"],
                    Path.Combine(Directory.GetCurrentDirectory(), "Schema")
                    ));

            Setup(arg => arg.InterchangeOrderFolder).As('i', "Interchange")
                .WithDescription("Path to a folder containing the Ed-Fi metadata files")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["InterchangeOrderFolder"],
                    Path.Combine(Directory.GetCurrentDirectory(), "Schema")
                    ));

            Setup(arg => arg.ApiLoaderExePath).As('l', "ApiLoaderExePath")
                .WithDescription("Path to EdFi.ApiLoader.Console executable")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["ApiLoaderExePath"],
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.MetadataUrl).As('m', "metadataurl")
                .WithDescription("The metadata url (i.e. http://server/metadata)")
                .SetDefault(ConfigurationManager.AppSettings["SwaggerUrl"]);
        }

        private static string GetFirstValue(params string[] defaults)
            => defaults.FirstOrDefault(x => !string.IsNullOrEmpty(x));

        private static T GetFirstValue<T>(params T[] defaults)
            => defaults.FirstOrDefault(x => !Equals(x, default(T)));
    }
}
