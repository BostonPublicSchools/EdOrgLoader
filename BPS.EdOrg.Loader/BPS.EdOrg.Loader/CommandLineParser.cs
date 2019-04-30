﻿using Fclp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    public class CommandLineParser : FluentCommandLineParser<Configuration>
    {
        public CommandLineParser()
        {
            Setup(arg => arg.CrossWalkOAuthUrl).As('u', "CrossWalkOAuthUrl")
               .WithDescription("The Crosswalk OAuth Url (i.e. http://server/api/v1.0)")
               .SetDefault(ConfigurationManager.AppSettings["CrossWalkOAuthUrl"]);

            Setup(arg => arg.CrossWalkSchoolApiUrl).As('p', "crosswalkApiUrl")
                .WithDescription("The Crosswalk web API url (i.e. http://server/api/v1.0)")
                .SetDefault(ConfigurationManager.AppSettings["CrossWalkSchoolApiUrl"]);

            Setup(arg => arg.CrossWalkKey).As('q', "key")
                 .WithDescription("The Crosswalk web API OAuth key")
                 .SetDefault(ConfigurationManager.AppSettings["CrossWalkKey"]);

            Setup(arg => arg.CrossWalkSecret).As('r', "secret")
                .WithDescription("The Crosswalk web API OAuth secret")
                .SetDefault(ConfigurationManager.AppSettings["CrossWalkSecret"]);

            Setup(arg => arg.ApiUrl).As('a', "apiurl")
                .WithDescription("The web API url (i.e. http://server/api/v1.0)")
                .SetDefault(ConfigurationManager.AppSettings["ApiUrl"]);

            Setup(arg => arg.SchoolYear).As('y', "year")
                .WithDescription("The target school year for the web API (i.e. 2016)")
                .SetDefault(GetFirstValue(
                        Convert.ToInt32(ConfigurationManager.AppSettings["SchoolYear"]),
                        DateTime.Today.Year));

            Setup(arg => arg.XMLOutputPath).As('d', "data")
                .WithDescription("Path to folder containing the XML data files to be submitted")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["XMLOutputPath"],
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.DataFilePath).As('b', "DataFilePath")
            .WithDescription("Path to folder containing the input text data files to be loaded")
            .SetDefault(GetFirstValue(
                ConfigurationManager.AppSettings["DataFilePath"],
                Directory.GetCurrentDirectory()));

            Setup(arg => arg.CrossWalkFilePath).As('c', "CrossWalkFilePath")
          .WithDescription("Path to folder containing the input text crosswalk files to be loaded")
          .SetDefault(GetFirstValue(
              ConfigurationManager.AppSettings["CrossWalkFilePath"],
              Directory.GetCurrentDirectory()));

            Setup(arg => arg.ApiLoaderExePath).As('l', "ApiLoaderPath")
                .WithDescription("Path to EdFi.ApiLoader.Console executable")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["ApiLoaderExePath"],
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.OauthKey).As('k', "Oauthkey")
                .WithDescription("The web API OAuth key")
                .SetDefault(ConfigurationManager.AppSettings["OAuthKey"]);

            Setup(arg => arg.MetadataUrl).As('m', "metadataurl")
                .WithDescription("The metadata url (i.e. http://server/metadata)")
                .SetDefault(ConfigurationManager.AppSettings["SwaggerUrl"]);

            Setup(arg => arg.OauthUrl).As('o', "oauthurl")
                .WithDescription("The OAuth url (i.e. http://server/oauth)")
                .SetDefault(ConfigurationManager.AppSettings["OAuthUrl"]);

            Setup(arg => arg.OauthSecret).As('s', "oauthsecret")
                 .WithDescription("The web API OAuth secret")
                 .SetDefault(ConfigurationManager.AppSettings["OAuthSecret"]);

            Setup(arg => arg.WorkingFolder).As('w', "working")
                .WithDescription("Path to a writable folder containing the working files")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["WorkingFolder"],
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.XsdFolder).As('x', "xsd")
                .WithDescription("Path to a folder containing the Ed-Fi Xsd Schema files")
                .SetDefault(GetFirstValue(
                    ConfigurationManager.AppSettings["XsdFolder"],
                    Path.Combine(Directory.GetCurrentDirectory(), "Schema")
                    ));
        }

        private static string GetFirstValue(params string[] defaults)
            => defaults.FirstOrDefault(x => !string.IsNullOrEmpty(x));

        private static T GetFirstValue<T>(params T[] defaults)
            => defaults.FirstOrDefault(x => !Equals(x, default(T)));
    }
}
