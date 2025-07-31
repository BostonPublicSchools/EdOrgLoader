using BPS.EdPlanLoaderCore.Controller;
using BPS.EdPlanLoaderCore.EdFi.Api;
using BPS.EdPlanLoaderCore.MetaData;
using BPS.EdPlanLoaderCore.Models;
using BPS.EdPlanLoaderCore.XMLDataLoad;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLineParser = BPS.EdPlanLoaderCore.MetaData.CommandLineParser;

namespace EdPlanLoaderCore
{
    
class Program
    {
        
        private static EdFiApiCrud edfiApi;
        private static StudentSpecialEducationController studentSpecController;
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Entry point of the application. Parses command line arguments, validates them,
        /// and executes core logic to process IEP and Alert files. Handles help requests
        /// and logs completion status and errors.
        /// </summary>
        /// <param name="args">Command line arguments provided to the application.</param>
        static void Main(string[] args)
        {
            // Configure log4net
            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));           
            logger.Info("Inside Main");

            // Build configuration with user secrets
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddUserSecrets<Program>();

            // Set the config in the static class
            AppSettings.Configuration = builder.Configuration;            
            
            // Initialize static fields
            edfiApi = new EdFiApiCrud(AppSettings.Configuration);
            studentSpecController = new StudentSpecialEducationController();

            // Parse command line arguments
            var parameter = new CommandLineParser();
            parameter.SetupHelp("?", "Help").Callback(text =>
            {
                Console.WriteLine(text);
                Environment.Exit(0);
            });

            var result = parameter.Parse(args);

            if (result.HasErrors)
            {
                Console.Write(result.ErrorText);
                Console.Write(parameter.Object.ErrorText);
                return;
            }

            try
            {
                LogConfiguration(parameter.Object);
                if(AppSettings.Configuration.GetValue<bool>("AppSettings:ShouldExecuteIEPLoad"))
                    RunIEPFile(parameter);
                if (AppSettings.Configuration.GetValue<bool>("AppSettings:ShouldExecuteAlertLoad"))
                    RunAlertFile(parameter);
                logger.Info("Job completed");
                
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());                
            }
        }

        /// <summary>
        /// Logs the current configuration values for diagnostic and auditing purposes.        
        /// </summary>
        /// <param name="configuration"></param>
        private static void LogConfiguration(EdorgConfiguration configuration)
        {
            logger.Info($"Api Url: {configuration.ApiUrl}");
            logger.Info($"Input Data Text File Path DataFilePathEdPlantoApen: {configuration.DataFilePathEdPlantoApen}");
            logger.Info($"Input Data Text File Path DataFilePathSpedSims: {configuration.DataFilePathSpedSims}");
            logger.Info($"Xsd Folder: {configuration.XsdFolder}");
           
        }

        private static void RunIEPFile(CommandLineParser param)
        {
           
            ParseXmls parseXmls = new ParseXmls(param.Object, logger);
                var token = edfiApi.GetAuthToken();

                if (!string.IsNullOrEmpty(token))
                {
                    // Use the same controller instance for all calls
                    var controller = new StudentSpecialEducationController();
                    controller.UpdateIEPSpecialEducationProgramAssociationDataAsync(token, parseXmls);
                    controller.UpdateEndDateSpecialEducation(
                        Constants.specialEdProgramTypeDescriptor,
                        token,
                        parseXmls,
                        controller.GetStudentsInIEPXml(parseXmls)
                    );
                }
                else
                {
                logger.Error("Token is not generated, ODS not updated");
                }
            }
        

        private static void RunAlertFile(CommandLineParser param)
        {
            
                ParseXmls parseXmls = new ParseXmls(param.Object, logger);
                var token = edfiApi.GetAuthToken();

                if (!string.IsNullOrEmpty(token))
                {
                    // Use the same controller instance for all calls
                    var controller = new StudentSpecialEducationController();
                    controller.UpdateAlertSpecialEducationData(token, parseXmls);
                    controller.UpdateEndDateSpecialEducation(
                        Constants.alertProgramTypeDescriptor,
                        token,
                        parseXmls,
                        controller.GetStudentsInAlertXml(parseXmls)
                    );
                }
                else
                {
                logger.Error("Token is not generated, ODS not updated");
                }
            }
        }
    }

