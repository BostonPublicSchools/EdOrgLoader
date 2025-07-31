using System;
using log4net;
using System.Diagnostics;
using System.Text;
using BPS.EdOrg.Loader.XMLDataLoad;
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;
using BPS.EdOrg.Loader.Controller;

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

                    //StaffAssociation data loaded to to ODS
                     RunJobCodeFile(param);
                    //Running IEP & Alert files files from PCG
                    RunIEPFile(param);
                    RunAlertFile(param);
                   
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
                
                Log.Info("Job completed");
            }
        }

        private static void LogConfiguration(EdorgConfiguration configuration)
        {
            Log.Info($"Api Url: {configuration.ApiUrl}");
            Log.Info($"CrossWalk Cross Walk OAuth Url:   {configuration.CrossWalkOAuthUrl}");
            Log.Info($"CrossWalk School Api Url:   {configuration.CrossWalkSchoolApiUrl}");
            Log.Info($"CrossWalk EdOrgServiceCenter Api Url:   {configuration.CrossWalkEdServicecenterApiUrl}");
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
            Log.Info($"Input Data Text File Path DataFilePathSpedSims:   {configuration.DataFilePathSpedSims}");
            Log.Info($"CrossWalk File Path: {configuration.CrossWalkFilePath}");
            Log.Info($"Working Folder: {configuration.WorkingFolder}");
            Log.Info($"Xsd Folder:  {configuration.XsdFolder}");
            Log.Info($"InterchangeOrder Folder:  {configuration.InterchangeOrderFolder}");
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
            if (Constants.ShouldExecuteStaffLoad)
            {
                var token = edfiApi.GetAuthToken();
                Log.Info("token retrieved" + token);
                if (token != null)
                {
                    staffController = new StaffAssociationController(token, param.Object, Log);
                    Log.Info("staff Association load Started...");
                    staffController.UpdateStaffAssociation(token, param.Object);

                }
                else Log.Error("Token is not generated, ODS not updated");
            }
               
        }
            
        

        private static void RunStaffContactFile(CommandLineParser param)
        {
            try
            {
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
        private static void RunIEPFile(CommandLineParser param)
        {
            if (Constants.ShouldExecuteIEPLoad)
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                //parseXmls.CreateXmlEdPlanToAspenTxt(); //Not reading from Edplan to Aspen file
                //parseXmls.CreateXmlSpedSimsTxt();
                var token = edfiApi.GetAuthToken();
                if (token != null)
                {
                    StudentSpecialEducationController controller = new StudentSpecialEducationController();
                    studentSpecController.UpdateIEPSpecialEducationProgramAssociationData(token, parseXmls);
                    studentSpecController.UpdateEndDateSpecialEducation(Constants.specialEdProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInIEPXml(parseXmls));


                }
                else Log.Error("Token is not generated, ODS not updated");
            }
        }

        private static void RunAlertFile(CommandLineParser param)
        {
            if (Constants.ShouldExecuteAlertLoad)
            {
                ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                var token = edfiApi.GetAuthToken();
                if (token != null)
                {
                    StudentSpecialEducationController controller = new StudentSpecialEducationController();
                    studentSpecController.UpdateAlertSpecialEducationData(token, parseXmls);
                    studentSpecController.UpdateEndDateSpecialEducation(Constants.alertProgramTypeDescriptor, token, parseXmls, controller.GetStudentsInAlertXml(parseXmls));

                }
                else Log.Error("Token is not generated, ODS not updated");
            }         

        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 204);
        }
                       

        
                
        
    }


    
}
