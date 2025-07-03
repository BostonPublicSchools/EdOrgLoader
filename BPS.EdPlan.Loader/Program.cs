using log4net;
using System;
using System.Diagnostics;
using BPS.EdPlanLoaderCore.Controller;
using BPS.EdPlanLoaderCore.EdFi.Api;
using Microsoft.CodeAnalysis;
using BPS.EdPlanLoaderCore.MetaData;
using CommandLineParser = BPS.EdPlanLoaderCore.MetaData.CommandLineParser;
using BPS.EdPlanLoaderCore.XMLDataLoad;

namespace EdPlanLoaderCore
{
    class Program
    {
        private static readonly Process Process = new Process();
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private static StudentSpecialEducationController studentSpecController = new StudentSpecialEducationController();

        /// <summary>
        /// Entry point of the application. Parses command line arguments, validates them,
        /// and executes core logic to process IEP and Alert files. Handles help requests
        /// and logs completion status and errors.
        /// </summary>
        /// <param name="args">Command line arguments provided to the application.</param>
        static void Main(string[] args)
        {
            var parameter = new CommandLineParser();
            parameter.SetupHelp("?", "Help").Callback(text =>
            {
                System.Console.WriteLine(text);
                Environment.Exit(0);
            });

            var result = parameter.Parse(args);

            if (result.HasErrors)
            {
                System.Console.Write(result.ErrorText);
                System.Console.Write(parameter.Object.ErrorText);
            }
            else
            {
                try
                {
                    LogConfiguration(parameter.Object);
                    // Execute core logic: run IEP and Alert file processing.
                    RunIEPFile(parameter);
                    RunAlertFile(parameter);

                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur during processing.
                    Log.Error(ex.Message);
                }

                // Log successful completion of the job.
                Log.Info("Job completed");
            }
        }

        /// <summary>
        /// Logs the current configuration values for diagnostic and auditing purposes.        
        /// </summary>
        /// <param name="configuration">
        private static void LogConfiguration(EdorgConfiguration configuration)
        {
            Log.Info($"Api Url: {configuration.ApiUrl}");                        
            Log.Info($"School Year: {configuration.SchoolYear}");           
            Log.Info($"Metadata Url:    {configuration.MetadataUrl}");
            Log.Info($"Data Folder: {configuration.XMLOutputPath}");
            Log.Info($"JobFilePath Folder: {configuration.JobFilePath}");
            Log.Info($"Input Data Text File Path:   {configuration.DataFilePath}");            
            Log.Info($"Input Data Text File Path DataFilePathEdPlantoApen:   {configuration.DataFilePathEdPlantoApen}");
            Log.Info($"Input Data Text File Path DataFilePathSpedSims:   {configuration.DataFilePathSpedSims}");            
            Log.Info($"Working Folder: {configuration.WorkingFolder}");
            Log.Info($"Xsd Folder:  {configuration.XsdFolder}");
            Log.Info($"InterchangeOrder Folder:  {configuration.InterchangeOrderFolder}");
        }

        // <summary>
        /// Processes the IEP file if enabled by configuration.
        /// Parses XML input, retrieves authentication token, and updates special education program data in the ODS.
        /// Logs errors if authentication fails.
        /// </summary>
        /// <param name="param">The command line parser containing parsed arguments and options.</param>
        private static void RunIEPFile(CommandLineParser param)
        {
            // Check if the configuration flag to execute IEP Load is set to true
            if (Constants.ShouldExecuteIEPLoad)
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);

                // Attempt to get an authentication token from the edfiApi
                var token = edfiApi.GetAuthToken();

                // If a token is successfully retrieved, proceed with file processing
                if (token != null)
                {
                    StudentSpecialEducationController controller = new StudentSpecialEducationController();
                    // Update IEP Special Education Program Association data 
                    studentSpecController.UpdateIEPSpecialEducationProgramAssociationDataAsync(token, parseXmls);
                    // Update the end date for the IEPs 
                    studentSpecController.UpdateEndDateSpecialEducation(Constants.specialEdProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInIEPXml(parseXmls));


                }
                // Log an error if the authentication token could not be retrieved
                else Log.Error("Token is not generated, ODS not updated");
            }
        }


        /// <summary>
        /// Processes the Alert file if enabled by configuration.
        /// Parses XML input, retrieves authentication token, and updates alert special education data in the ODS.
        /// Logs an error if authentication fails.
        /// </summary>
        /// <param name="param">The command line parser containing parsed arguments and options.</param>
        private static void RunAlertFile(CommandLineParser param)
        {
            // Check if the configuration flag to execute Alert Load is set to true
            if (Constants.ShouldExecuteAlertLoad)
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);

                // Attempt to get an authentication token from the edfiApi
                var token = edfiApi.GetAuthToken();

                // If a token is successfully retrieved, proceed with file processing
                if (token != null)
                {
                    StudentSpecialEducationController controller = new StudentSpecialEducationController();
                    // Update Alert Special Education data
                    studentSpecController.UpdateAlertSpecialEducationData(token, parseXmls);
                    // Update the end date for alert programs
                    studentSpecController.UpdateEndDateSpecialEducation(Constants.alertProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInAlertXml(parseXmls));

                }
                // Log an error if the authentication token could not be retrieved
                else Log.Error("Token is not generated, ODS not updated");
            }

        }

    }
}
