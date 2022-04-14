using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.XMLDataLoad;
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;
using BPS.EdOrg.Loader.Controller;
using System.DirectoryServices;

namespace BPS.EdOrg.Loader
{
    class Program
    {
        private static readonly Process Process = new Process();
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private static StudentSpecialEducationController studentSpecController = new StudentSpecialEducationController();
        public static ParseXmls parseXmls = null;
        public static Notification notification;
        public static StaffAssociationController staffController;
       
        static void Main(string[] args)
        {
            var param = new CommandLineParser();
            param.SetupHelp("?", "Help").Callback(text =>
            {
                System.Console.WriteLine(text);
                Environment.Exit(0);
            });

            var result = param.Parse(args);

            if (result.HasErrors || !param.Object.IsValid)


            {
                System.Console.Write(result.ErrorText);
                System.Console.Write(param.Object.ErrorText);
            }
            else
            {
                try
                {
                    parseXmls = new ParseXmls(param.Object, Log);                    
                    LogConfiguration(param.Object);

                    // creating the xml and executing the file through command line parser                    
                    RunDeptFile(param);
                    RunStaffEmail(param);
                    RunJobCodeFile(param);
                    RunStaffAddressFile(param);
                    RunStaffContactFile(param);
                    RunTransferCasesFile(param);
                    RunAlertIEPFile(param);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
                finally
                {
                    //Archiving the file for comarison 
                    parseXmls.Archive(param.Object);
                }
                
                Log.Info("Job completed");
            }
        }

        private static void LogConfiguration(EdorgConfiguration configuration)
        {
            Log.Info($"Api Url: {configuration.ApiUrl}");
            Log.Info($"CrossWalk Cross Walk OAuth Url:   {configuration.CrossWalkOAuthUrl}");
            Log.Info($"CrossWalk School Api Url:   {configuration.CrossWalkSchoolApiUrl}");
            Log.Info($"CrossWalk Staff Api Url:   {configuration.CrossWalkStaffApiUrl}");
            Log.Info($"School Year: {configuration.SchoolYear}");
            Log.Info($"CrossWalk Key:   {configuration.CrossWalkKey}");
            Log.Info($"CrossWalk Secret:   {configuration.CrossWalkSecret}");
            Log.Info($"Oauth Key:   {configuration.OauthKey}");
            Log.Info($"OAuth Secret:   {configuration.OauthSecret}");
            Log.Info($"Metadata Url:    {configuration.MetadataUrl}");
            Log.Info($"Data Folder: {configuration.XMLOutputPath}");
            Log.Info($"JobFilePath Folder: {configuration.JobFilePath}");
            Log.Info($"Input Data Text File Path:   {configuration.DataFilePath}");
            Log.Info($"Input Data Text File Path Job:   {configuration.DataFilePathJob}");
            Log.Info($"Input Data Text File Path Job:   {configuration.DataFilePathJobPreviousFile}");
            Log.Info($"Input Data Text File Path DataFilePathJobTransfer:   {configuration.DataFilePathJobTransfer}");
            Log.Info($"Input Data Text File Path DataFilePathStaffPhoneNumbers:   {configuration.DataFilePathStaffPhoneNumbers}");
            Log.Info($"Input Data Text File Path DataFilePathEdPlantoApen:   {configuration.DataFilePathEdPlantoApen}");
            Log.Info($"CrossWalk File Path: {configuration.CrossWalkFilePath}");
            Log.Info($"Working Folder: {configuration.WorkingFolder}");
            Log.Info($"Xsd Folder:  {configuration.XsdFolder}");
            Log.Info($"InterchangeOrder Folder:  {configuration.InterchangeOrderFolder}");
        }
        private static void LoadXml(EdorgConfiguration configuration)
        {
            try
            {
                Log.Info($"Started executing EdFi.ApiLoader.Console from path :{configuration.ApiLoaderExePath}");
                Process.EnableRaisingEvents = true;
                Process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_OutputDataReceived);
                Process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_ErrorDataReceived);
                Process.Exited += new System.EventHandler(process_Exited);
                Process.StartInfo.FileName = configuration.ApiLoaderExePath;
                Process.StartInfo.Arguments = GetArguments(configuration);
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.RedirectStandardError = true;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.Start();
                Process.BeginErrorReadLine();
                Process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception while executing EdFi.ApiLoader.Console : {ex.Message}");
            }
        }
        private static string GetArguments(EdorgConfiguration configuration)
        {
            StringBuilder argumentBuilder = new StringBuilder();
            try
            {
                argumentBuilder.Append($"/a {configuration.ApiUrl} ");
                argumentBuilder.Append($"/d {configuration.XMLOutputPath} ");
                argumentBuilder.Append($"/v {configuration.JobFilePath} ");
                argumentBuilder.Append($"/k {configuration.OauthKey} ");
                argumentBuilder.Append($"/s {configuration.OauthSecret} ");
                argumentBuilder.Append($"/m {configuration.MetadataUrl} ");
                argumentBuilder.Append($"/o {configuration.OauthUrl} ");
                argumentBuilder.Append($"/x {configuration.XsdFolder} ");
                argumentBuilder.Append($"/i {configuration.InterchangeOrderFolder} ");
                argumentBuilder.Append($"/w {configuration.WorkingFolder} ");
                argumentBuilder.Append($"/y {configuration.SchoolYear}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in parsing arguments :{ex.Message}");
            }
            return argumentBuilder.ToString();
        }
        private static void process_Exited(object sender, EventArgs e)
        {
            Log.Info($"process exited with code {Process.ExitCode.ToString()}");
        }
        private static void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.Error($"Error while running API loader : {e.Data}");
        }
        private static void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.Info(e.Data);
        }
        private static void RunDeptFile(CommandLineParser param)
        {

            //For Dept_tbl.txt           
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            parseXmls.CreateXml();           
            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                staffController = new StaffAssociationController(token, param.Object, Log);
                staffController.UpdateEducationServiceCenter(token, param.Object);

            }

        }
        private static void RunStaffEmail(CommandLineParser param)
        {

            //For Dept_tbl.txt           
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            parseXmls.CreateXmlStaffEmail();
            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                staffController = new StaffAssociationController(token, param.Object, Log);
                staffController.UpdateStaffEmailData(token, param.Object);
            }
        }

        
        private static void RunJobCodeFile(CommandLineParser param)
        {

            // For JobCode_tbl.txt
            //List<string> existingStaffId = GetStaffList(param.Object);
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            parseXmls.CreateXmlJob();

            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                staffController = new StaffAssociationController(token, param.Object, Log);
                staffController.UpdateStaffEmploymentAssociationData(token, param.Object);               
                staffController.UpdateStaffAssignmentAssociationData(token, param.Object);
            }

            else Log.Error("Token is not generated, ODS not updated");


        }

        private static void RunStaffContactFile(CommandLineParser param)
        {
            try
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                parseXmls.CreateXmlStaffContact();

                var token = edfiApi.GetAuthToken();
                if (token != null)
                {
                    staffController = new StaffAssociationController(token, param.Object, Log);
                    staffController.UpdateStaffContact(token, param.Object);
                }

                else Log.Error("Token is not generated, ODS not updated");
            }
            catch (Exception ex)
            {
                notification = new Notification(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }


        }

        private static void RunStaffAddressFile(CommandLineParser param)
        {
            try
            {

                ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                parseXmls.CreateXmlStaffAddress();
                var token = edfiApi.GetAuthToken();
                if (token != null)
                {
                    staffController = new StaffAssociationController(token, param.Object, Log);
                    staffController.UpdateStaffAddress(token, param.Object);
                }
                else Log.Error("Token is not generated, ODS not updated");
            }
            catch (Exception ex)
            {
                notification = new Notification(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
        }
        private static void RunTransferCasesFile(CommandLineParser param)
        {
            try
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                parseXmls.CreateXmlTransferCases();

                var token = edfiApi.GetAuthToken();
                if (token != null)
                {
                    staffController = new StaffAssociationController(token, param.Object, Log);
                    staffController.UpdateStaffAssignmentDataTransferCases(token, param.Object);
                }

                else Log.Error("Token is not generated, ODS not updated");
            }
            catch(Exception ex)
            {
                notification = new Notification(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            

        }
        private static void RunAlertIEPFile(CommandLineParser param)
        {
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            //parseXmls.CreateXmlEdPlanToAspenTxt();
            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                StudentSpecialEducationController controller = new StudentSpecialEducationController();
                studentSpecController.UpdateAlertSpecialEducationData(token, parseXmls);                
                studentSpecController.UpdateEndDateSpecialEducation(Constants.alertProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInAlertXml(parseXmls));
                studentSpecController.UpdateIEPSpecialEducationProgramAssociationData(token, parseXmls);
                studentSpecController.UpdateEndDateSpecialEducation(Constants.specialEdProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInIEPXml(parseXmls));

            }
            else Log.Error("Token is not generated, ODS not updated");

        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 204);
        }
                       

        
                
        
    }
}
