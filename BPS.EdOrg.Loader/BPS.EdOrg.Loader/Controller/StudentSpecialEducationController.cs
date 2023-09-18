using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Xml.Linq;
using System.Linq;
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.XMLDataLoad;
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;
using Formatting = Newtonsoft.Json.Formatting;
using System.Globalization;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Runtime.Remoting.Contexts;

namespace BPS.EdOrg.Loader.Controller
{
    class StudentSpecialEducationController
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private static Notification notification;
        public void UpdateAlertSpecialEducationData(string token, ParseXmls prseXMl)
        {

            try
            {
                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"/504inXML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                var doc = XDocument.Parse(fragments);
                XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//root/student");
                foreach (XmlNode node in nodeList)
                {
                    var studentSpecialEducationList = GetAlertXml(node);

                    if (studentSpecialEducationList.EducationOrganizationId != null && studentSpecialEducationList.Name != null && studentSpecialEducationList.Type != null)
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, studentSpecialEducationList.ProgramEducationOrganizationId, studentSpecialEducationList.Name, studentSpecialEducationList.Type);
                        if (studentSpecialEducationList.StudentUniqueId != null)
                        {
                            if (!string.IsNullOrEmpty(studentSpecialEducationList.BeginDate))                                
                                InsertAlertDataSpecialEducation(token, studentSpecialEducationList);                            

                            if (!string.IsNullOrEmpty(studentSpecialEducationList.IepExitDate))
                                UpdateAlertStudentSpecialEducation(token, studentSpecialEducationList);
                        }
                       
                    }
                }

                if (File.Exists(Constants.LOG_FILE))
                    notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }
        public void UpdateEndDateSpecialEducation(string specialEdPlan, string token, ParseXmls prseXMl, List<string> students)
        {
            try
            {
                int offset = 0;
                string endDate = null;
                List<UpdateEndDateStudent> studentSpecialEducations = null;
                           
                // Get the ODS data 
                studentSpecialEducations = GetStudentSpecialEducation(specialEdPlan, token, offset);

                while (studentSpecialEducations != null && studentSpecialEducations.Any())
                {
                    if(students != null && students.Any())
                    {
                        var studentsNotInXml = studentSpecialEducations.Where(t2 => students.All(t1 => !t2.studentReference.studentUniqueId.Equals(t1)) && (t2.EndDate == null));
                        foreach (var item in studentsNotInXml)
                        {                                                        
                            // Set enddate in case the sTudent doesn't exist in IEP file anymore
                            endDate = DateTime.Now.ToString();
                            if (!String.IsNullOrEmpty(endDate))
                                item.EndDate = endDate.Split()[0];
                            SetEndDate(specialEdPlan, token, item);
                            Console.WriteLine("Offset" + offset);
                        }
                    }

                    var studentsInXml = studentSpecialEducations.Where(t2 => t2.EndDate == null).ToList();
                    foreach (var item in studentsInXml)
                    {
                        
                        // Set enddate for previous records in ODS
                        endDate = GetEndDateProgramAssociation(specialEdPlan, token, item);
                        if (!String.IsNullOrEmpty(endDate))
                            item.EndDate = endDate.Split()[0];
                        SetEndDate(specialEdPlan, token, item);
                        //offset 16100 has some issue skipping the offset and loading again
                        if (studentSpecialEducations == null)
                        {
                            offset = offset + 1000;
                            studentSpecialEducations = GetStudentSpecialEducation(specialEdPlan, token, offset);
                        }
                        Console.WriteLine("Offset" + offset);
                    }
                    offset = offset + 1000;
                    studentSpecialEducations = GetStudentSpecialEducation(specialEdPlan, token, offset);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }

        public List<string> GetStudentsInIEPXml(ParseXmls prseXMl) {

            List<string> students = new List<string>();
            try
            {
                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    var fragments = File.ReadAllText(file.FullName).Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "").Replace("&", "&amp;");
                    var doc = XDocument.Parse(fragments);
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");
                    foreach (XmlNode node in nodeList)
                    {
                        // Parsing PCG IEP XML to get student data 
                        var Id = GetSpecialEducationXml(node);
                        students.Add(Id.studentUniqueId);
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return students;
        }

        public List<string> GetStudentsInAlertXml(ParseXmls prseXMl)
        {

            List<string> students = new List<string>();
            try
            {
                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"/504inXML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    var doc = XDocument.Parse(fragments);
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                    //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                    //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/student");
                    foreach (XmlNode node in nodeList)
                    {
                        var studentSpecialEducationList = GetAlertXml(node);
                        students.Add(studentSpecialEducationList.StudentUniqueId);
                    }                   

                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return students;
        }

        public static List<UpdateEndDateStudent> GetStudentSpecialEducation(string specialEdPlan, string token,int offset = 0)
        {
            List<UpdateEndDateStudent> data =  null;
            try
            {
                if (token != null)
                {
                    var client = offset == 0 ? new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + specialEdPlan)
                                       : new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + specialEdPlan + "&offset="+ offset);

                    //var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit+ specialEdPlan + Constants.SpecEduStudentUniqueId + "355639");
                                          
                    var response = edfiApi.GetData(client, token);

                    if (IsSuccessStatusCode((int)response.StatusCode))                                           
                         data = JsonConvert.DeserializeObject<List<UpdateEndDateStudent>>(response.Content);
                       
                    
                }
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);                
            }
            return data;
        }

        

        
        public  void UpdateIEPSpecialEducationProgramAssociationData(string token, ParseXmls prseXMl)
        {
            try
            {

                if (!Directory.Exists(ConfigurationManager.AppSettings["XMLExtractedPath"]))
                    Directory.CreateDirectory(ConfigurationManager.AppSettings["XMLExtractedPath"]);

                foreach (System.IO.FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                file.Delete();
                ZipFile.ExtractToDirectory(ConfigurationManager.AppSettings["XMLDeploymentPath"] + ConfigurationManager.AppSettings["XMLZip"], ConfigurationManager.AppSettings["XMLExtractedPath"]);
                
                // parsing spedsims attributes from spedSims file
                XmlDocument xmlDocSpedsims = prseXMl.LoadXml("SpedSims");

                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    var fragments = File.ReadAllText(file.FullName).Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "").Replace("&", "&amp;");
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(XDocument.Parse(fragments));
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");
                    //await Task.Run(() => ProcessIEPXml(nodeList, xmlDocSpedsims, token));  
                    ProcessIEPXml(nodeList, xmlDocSpedsims, token);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
       

        private static void ProcessIEPXml(XmlNodeList nodeList, XmlDocument xmlDocSpedsims, string token)
        {
            //var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 10 };
            foreach (XmlNode node in nodeList)   
            //Parallel.ForEach< XmlNodeList>(nodeList, new ParallelOptions { MaxDegreeOfParallelism = 2 }, node =>

            {

                // Parsing PCG IEP XML to get IEP data 
                var spEducationService = GetSpecialEducationXml(node);
                Console.WriteLine("processing for studentID : " + spEducationService.studentUniqueId);
                // In case of Duplicate Services in Xml the api request is not successfully posted through the api
                var spEducation = CheckDuplicateServices(spEducationService);

                //Getting Import diabilty desc from sims file
                var getStudentSpedInfo = xmlDocSpedsims.SelectNodes(@"//root/iep/studentNumber").Cast<XmlNode>().Where(a => a.InnerText == spEducation.studentUniqueId).Select(x => x.ParentNode).ToList();
                if (getStudentSpedInfo.Count > 0)
                    foreach (var item in getStudentSpedInfo)
                    {
                        var studentSped = GetStudentSpedXml(item);
                        spEducation.levelofNeed = Constants.GetLevelOfNeed(studentSped.levelNeedInfo);
                        spEducation.disability = Constants.GetDisabilityDescriptor(studentSped.disabilityInfo);
                    }

                //Check required field exist in XML source 
                if (!string.IsNullOrEmpty(spEducation.programEducationOrganizationId) && !string.IsNullOrEmpty(spEducation.programName) && !string.IsNullOrEmpty(spEducation.programTypeDescriptorId) && !string.IsNullOrEmpty(spEducation.studentUniqueId))
                {
                    // Check if the Program already exists in the ODS if not first enter the Progam.
                    VerifyProgramData(token, spEducation.programEducationOrganizationId, spEducation.programName, spEducation.programTypeDescriptorId);

                    // Insert if SignatureDate, IepBeginDate,IepEndDate is not null
                    if (!string.IsNullOrEmpty(spEducation.beginDate) && !string.IsNullOrEmpty(spEducation.iepBeginDate) && !string.IsNullOrEmpty(spEducation.iepEndDate))
                        //await Task.Run(() => InsertIEPStudentSpecialEducation(token, spEducation));
                        InsertIEPStudentSpecialEducation(token, spEducation);

                    if (!string.IsNullOrEmpty(spEducation.iepExitDate))
                        //await Task.Run(() => UpdateIEPStudentSpecialEducation(token, spEducation));
                        UpdateIEPStudentSpecialEducation(token, spEducation);


                }
                else
                {
                    Log.Info("Required fields are empty for studentUniqueId:" + spEducation.studentUniqueId);
                }
            }
            if (File.Exists(Constants.LOG_FILE))
                notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
        }
    





        private static StudentSpecialEducationProgramAssociation CheckDuplicateServices(StudentSpecialEducationProgramAssociation spEducationServices)
        {
            //Checking iep file with similar serviceDescriptor and calculating service duration for same
             var dupService = spEducationServices.relatedServices.GroupBy(x => new { x.SpecialEducationProgramServiceDescriptor, x._ext.myBPS.serviceLocation })
              .Where(g => g.Count() > 1).SelectMany(y => y).ToList();           

            var dupServiceDis = spEducationServices.relatedServices.GroupBy(x => new { x.SpecialEducationProgramServiceDescriptor, x._ext.myBPS.serviceLocation })
              .Where(g => g.Count() > 1).Select(y => y.First()).ToList();

            // multiple similar service descriptor 
            if (dupServiceDis != null && dupServiceDis.Any())
            {

                foreach (var item in dupServiceDis)
                {                   
                        var itemToRemove = spEducationServices.relatedServices
                        .RemoveAll(r => r.SpecialEducationProgramServiceDescriptor == item.SpecialEducationProgramServiceDescriptor && r._ext.myBPS.serviceLocation == item._ext.myBPS.serviceLocation);

                    var dupSingleService = dupService.Where(x => x.SpecialEducationProgramServiceDescriptor == item.SpecialEducationProgramServiceDescriptor).ToList();
                    item._ext.myBPS.serviceDuration = GetTotalMinutes(dupSingleService).ToString();
                    item._ext.myBPS.serviceDurationRecurrenceDescriptor = "day";
                    item._ext.myBPS.serviceDurationFrequency = "1";

                    spEducationServices.relatedServices.Add(item);
                    Log.Info("Duplicate ServiceDescriptor exist for student  : " + spEducationServices.studentUniqueId + "Services " + item.SpecialEducationProgramServiceDescriptor);
                }
                

            }

            return spEducationServices;
        }

        private static int GetTotalMinutes(List<Service> dupService)
        {
            var TotalMinutes = 0;
            
            #region Dictionary with unit type and its value
            var unitTypeList = new Dictionary<string, int>();
            unitTypeList.Add("minute(s)", 1);
            unitTypeList.Add("hour(s)", 60);
            #endregion

            #region Dictionary with recurrence type and its value
            var recurrenceTypeList = new Dictionary<string, int>();

            recurrenceTypeList.Add("5-day cycle", 5);
            recurrenceTypeList.Add("6-day cycle", 6);
            recurrenceTypeList.Add("7-day cycle", 7);
            recurrenceTypeList.Add("week", 5);
            recurrenceTypeList.Add("day", 1);
            recurrenceTypeList.Add("month", 20);
            recurrenceTypeList.Add("school year", 240);
            #endregion

            try
            {
                //Adding up Minutes
                foreach (var item in dupService)
                {
                    var serviceDurationUnit = string.Empty;
                    serviceDurationUnit = item._ext.myBPS.serviceDurationUnitDescriptor;
                    var unitIndex = serviceDurationUnit != null ? serviceDurationUnit.LastIndexOf("#") : -1;
                    if (unitIndex >= 0)
                    {
                        serviceDurationUnit = serviceDurationUnit.Substring(unitIndex + 1);
                    }
                    var serviceMinutes = ((int.Parse(item._ext.myBPS.serviceDuration) * unitTypeList[serviceDurationUnit] *
                                                        (int.Parse(item._ext.myBPS.serviceDurationFrequency) == 0 ? 1 : int.Parse(item._ext.myBPS.serviceDurationFrequency))
                                                     / recurrenceTypeList[item._ext.myBPS.serviceDurationRecurrenceDescriptor]));

                    TotalMinutes += serviceMinutes;


                }


                return TotalMinutes;
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
                return TotalMinutes;
            }
        }




            public static IRestResponse VerifyProgramData(string token, string programEdOrgId, string programName, string programType)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program+ Constants.educationOrganizationId + programEdOrgId + Constants.programName + programName + Constants.programType + programType);

                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length <= 2)
                    {
                        var rootObject = new EdFiProgram
                        {
                            EducationOrganizationReference = new EdFiEducationReference
                            {
                                educationOrganizationId = programEdOrgId,
                                Link = new Link()
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },
                            ProgramId = null,
                            ProgramTypeDescriptor = "uri://ed-fi.org/ProgramTypeDescriptor#" + programType,
                            SponsorType = string.Empty,
                            ProgramName = programName,


                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = edfiApi.PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program), token);  
                        Log.Info("Verify if the program data exists in EdfI Program for programTypeId : " + programType);
                    }
                    
                }
                return response;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting the program data :" + ex);
                return null;
            }


        }



        private static bool IsSuccessStatusCode(int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 204);
        }
        
        private static string GetEndDateProgramAssociation(string specialEdPlan,string token, UpdateEndDateStudent spList)
        {
            string endDate = DateTime.Now.ToString();
            try
            {
                
                IRestResponse response = null;                 
                var client =new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation+ Constants.studentUniqueId + spList.studentReference.studentUniqueId + specialEdPlan);
                response = edfiApi.GetData(client, token);
                if (response.Content.Length > 2)
                {
                    List<SpecialEducationReference> data = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                    if (data.Count() >= 2)
                    {
                        DateTime beginDate;
                        DateTime.TryParse(spList.BeginDate, out beginDate);
                        DateTime maxValue = default(DateTime);
                        foreach (var item in data)
                        {
                            DateTime inputDateTime;
                            DateTime.TryParse(item.beginDate, out inputDateTime);
                            int result = DateTime.Compare(inputDateTime, maxValue);
                            if (result >= 0)                            
                                maxValue = inputDateTime;                            
                                                       
                        }
                        //Compare BeginDate
                        int resultDate = DateTime.Compare(beginDate, maxValue);
                        if (resultDate >= 0)
                            endDate = null;
                        else
                            endDate = maxValue.ToString();
                    }
                
                    else
                        endDate = null;
                }
                   

                return endDate;
            }
            

            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
                return endDate;
            }


        }
        private static string GetBeginDate(string token, SpecialEducation specialEd)
        {
            IRestResponse response = null;
            string beginDate = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + specialEd.StudentUniqueId + "&programName=504 Plan"+ "&programEducationOrganizationId="+specialEd.EducationOrganizationId);
                response = edfiApi.GetData(client, token);
                var data = JsonConvert.DeserializeObject<List<SpecialEducation>>(response.Content);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        foreach (var item in data)
                        {

                            beginDate = item.BeginDate;

                        }
                    }
                }
                return beginDate;
            }
            catch(Exception ex)
            {
                Log.Error("Something went wrong while getting BeginDate" + ex.Message);
                return beginDate;
            }
            
        }

        
        private static void SetEndDate(string specialEdPlan,string token, UpdateEndDateStudent spItem)
        {           
            try
            {    IRestResponse response = null;                
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + spItem.studentReference.studentUniqueId + specialEdPlan + Constants.beginDate+ spItem.BeginDate);
                response = edfiApi.GetData(client, token);
                //dynamic original = JsonConvert.DeserializeObject(response.Content);
                List<SpecialEducationReference> original = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                if (response.Content.Length > 2)
                {
                    foreach (var data in original)
                    {
                        data.endDate = spItem.EndDate;
                        string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                        var id = data.id;
                           var resp = edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);

                        
                    }
                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
                
            }


        }

        
        private static void InsertAlertDataSpecialEducation(string token, SpecialEducation spList)
        {
            IRestResponse response = null;
            try
            {
                var rootObject = GetAlertSpecialEducation(token, spList);
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine("Inside Alert SpecialED");
               // check for Student and BeginDate 
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + spList.StudentUniqueId + "&programName=504 Plan" + Constants.beginDate + spList.IepSignatureDate);
                response = edfiApi.GetData(client, token);
                dynamic original = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                      
                        foreach (var data in original)
                        {
                            //string dataSource = null;
                            var id = data.id;
                            //if (data._ext != null) dataSource = data._ext.myBPS.dataSource;
                            //rootObject._ext.myBPS.dataSource = Constants.DataSourceXml;
                            string stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate = Convert.ToDateTime(data.beginDate) ?? null;
                            
                            if (id != null)
                            {
                                
                                if (spList.IepSignatureDate != null)
                                {
                                    if (stuId != null && iepDate != null)
                                    {
                                        
                                        DateTime inputDateTime;
                                        if (DateTime.TryParse(spList.IepSignatureDate, out inputDateTime))
                                        {
                                            var result = DateTime.Compare(inputDateTime, iepDate);
                                            if (stuId == spList.StudentUniqueId && result == 0)
                                            {
                                                if (spList.ProgramEducationOrganizationId == "" || spList.ProgramEducationOrganizationId.Equals(null)) spList.ProgramEducationOrganizationId = Constants.educationOrganizationIdValue;
                                                if (DateTime.Parse(data.beginDate, CultureInfo.InvariantCulture) != DateTime.Parse(spList.BeginDate, CultureInfo.InvariantCulture) ||!data.programReference.educationOrganizationId
                                                    .Equals(spList.ProgramEducationOrganizationId.TrimStart('0')) || !data.programReference.ProgramName.Equals(spList.Name))
                                                {
                                                    var updateResponse = edfiApi.DeleteData(new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                                    if (IsSuccessStatusCode((int)response.StatusCode)) updateResponse = edfiApi.PostData(json, client, token);
                                                    Log.Info("Update StudentSpecialEdOrg : " + "studentUniqueId" + stuId + "IepSignatureDate" + spList.IepSignatureDate);
                                                }
                                                                                                    
                                            }
                                                
                                            else
                                            {
                                                var updateResponse = edfiApi.PostData(json, client, token);
                                                Log.Info("Insert StudentSpecialEdOrg : " + "studentUniqueId" + stuId + "IepSignatureDate" + spList.IepSignatureDate);
                                            }
                                               

                                        }
                                    }
                                    

                                }

                            }                 
                        }
                    }

                    else
                    {
                        response = edfiApi.PostData(json, client, token);
                        Log.Info("Inserting if record doesn't exist StudentSpecialEdOrg : " + "studentUniqueId" + spList.StudentUniqueId);
                    }
                                           
                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }


        }
        private static EdFiStudentSpecialEducation GetSpecialEducation(string token, StudentSpecialEducationProgramAssociation spList)
        {
            EdFiStudentSpecialEducation specialEducation = new EdFiStudentSpecialEducation();
            try
            {
                specialEducation = new EdFiStudentSpecialEducation
                {
                    
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    programReference = new ProgramReference
                    {
                        educationOrganizationId = spList.programEducationOrganizationId,
                        programTypeDescriptor = "uri://ed-fi.org/ProgramTypeDescriptor#" + spList.programTypeDescriptorId,
                        ProgramName = spList.programName,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    studentReference = new StudentReference
                    {
                        studentUniqueId = spList.studentUniqueId,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }

                    },
                    disabilities = new List<Disabilities>(),         
                    ideaEligibility = spList.ideaEligibility,
                    iepReviewDate = spList.iepReviewDate,
                    iepBeginDate = spList.iepBeginDate,
                    iepEndDate = spList.iepEndDate,
                    lastEvaluationDate = spList.lastEvaluationDate,
                    beginDate = spList.beginDate,
                    schoolHoursPerWeek = spList.schoolHoursPerWeek,
                    specialEducationHoursPerWeek = spList.specialEducationHoursPerWeek,
                    specialEducationSettingDescriptor = spList.specialEducationSettingDescriptorId,
                    specialEducationProgramServices = new List<Service>(),
                    _ext = new EdFiExt()
                    {
                        myBPS = new Ext()
                        {
                            iepExitDate = spList.iepExitDate,
                            parentResponse = spList.parentResponse,
                            costSharingAgency = spList.costSharingAgency,
                            isCostSharing = spList.isCostSharing,
                            //dataSource = spList.dataSource,
                            sourceSystemId = spList.iepUniqueId,
                            levelOfNeedDescriptor = spList.levelofNeed
                        }
                    }

                };


                if (spList.disability != null)
                {
                    var disability = new Disabilities
                    {
                        disabilityDescriptor = spList.disability,
                        orderOfDisability = 1
                    };
                    specialEducation.disabilities.Add(disability);
                }

                if (spList.relatedServices != null)

                    foreach (var serviceItem in spList.relatedServices)
                        //.GroupBy(p => p.SpecialEducationProgramServiceDescriptor)
                        // .Select(grp => grp.First())
                        //   .ToList())
                    {
                        var serLocation = Constants.GetServiceLocation(serviceItem._ext.myBPS.serviceLocation);
                        var array = new[] { serviceItem.SpecialEducationProgramServiceDescriptor, serLocation };                       
                        var ServiceDescLocation = string.Join(" - ", array.Where(s => !string.IsNullOrEmpty(s))); 
                        
                        PostServiceDescriptor(ServiceDescLocation, token);
                        var relatedService = new Service
                        {
                            PrimaryIndicator = false, // default is false
                            SpecialEducationProgramServiceDescriptor = "uri://mybps.org/SpecialEducationProgramServiceDescriptor#" +$"{string.Concat(ServiceDescLocation.ToString().Trim().Take(50))}",
                            ServiceBeginDate = serviceItem.ServiceBeginDate,
                            ServiceEndDate = serviceItem.ServiceEndDate,
                            _ext = new EdFiExtension()
                            {
                                myBPS =
                            new Extension
                            {
                                serviceDuration = serviceItem._ext.myBPS.serviceDuration,
                                serviceDurationFrequency = serviceItem._ext.myBPS.serviceDurationFrequency,
                                serviceDurationRecurrenceDescriptor = "uri://mybps.org/ServiceDurationRecurrenceDescriptor#" + Constants.GetSDRecurrenceDesc(serviceItem._ext.myBPS.serviceDurationRecurrenceDescriptor),
                                serviceDurationUnitDescriptor = "uri://mybps.org/ServiceDurationUnitDescriptor#" + Constants.GetSDUnitDesc(serviceItem._ext.myBPS.serviceDurationUnitDescriptor),
                                serviceLocation = serviceItem._ext.myBPS.serviceLocation,
                                serviceClass = serviceItem._ext.myBPS.serviceClass
                            }
                            }
                                

                        };
                        
                        specialEducation.specialEducationProgramServices.Add(relatedService);
                    }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
            return specialEducation;
        }

        private static EdFiStudentSpecialEducation GetAlertSpecialEducation(string token, StudentSpecialEducationProgramAssociation spList)
        {
            EdFiStudentSpecialEducation specialEducation = new EdFiStudentSpecialEducation();
            try
            {
                specialEducation = new EdFiStudentSpecialEducation
                {
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    programReference = new ProgramReference
                    {
                        educationOrganizationId = spList.programEducationOrganizationId,
                        programTypeDescriptor = "uri://ed-fi.org/ProgramTypeDescriptor#" + spList.programTypeDescriptorId,
                        ProgramName = spList.programName,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    studentReference = new StudentReference
                    {
                        studentUniqueId = spList.studentUniqueId,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }

                    },
                    ideaEligibility = spList.ideaEligibility,
                    iepReviewDate = spList.iepReviewDate,
                    iepBeginDate = spList.iepBeginDate,
                    iepEndDate = spList.iepEndDate,
                    lastEvaluationDate = spList.lastEvaluationDate,
                    beginDate = spList.beginDate,
                    schoolHoursPerWeek = spList.schoolHoursPerWeek,
                    specialEducationHoursPerWeek = spList.specialEducationHoursPerWeek,
                    specialEducationSettingDescriptor = spList.specialEducationSettingDescriptorId,
                    specialEducationProgramServices = new List<Service>(),
                    _ext = new EdFiExt()
                    {
                        myBPS = new Ext()
                        {
                            iepExitDate = spList.iepExitDate,
                            parentResponse = spList.parentResponse,
                            costSharingAgency = spList.costSharingAgency,
                            isCostSharing = spList.isCostSharing

                        }
                    }

                };

                if (spList.relatedServices != null)

                    foreach (var serviceItem in spList.relatedServices)
                    //.GroupBy(p => p.SpecialEducationProgramServiceDescriptor)
                    // .Select(grp => grp.First())
                    //   .ToList())
                    {
                        var serLocation = Constants.GetServiceLocation(serviceItem._ext.myBPS.serviceLocation);
                        var array = new[] { serviceItem.SpecialEducationProgramServiceDescriptor, serLocation };
                        var ServiceDescLocation = string.Join(" - ", array.Where(s => !string.IsNullOrEmpty(s)));

                        PostServiceDescriptor(ServiceDescLocation, token);
                        var relatedService = new Service
                        {
                            PrimaryIndicator = false, // default is false
                            SpecialEducationProgramServiceDescriptor = "uri://mybps.org/SpecialEducationProgramServiceDescriptor#" + $"{string.Concat(ServiceDescLocation.ToString().Trim().Take(50))}",
                            ServiceBeginDate = serviceItem.ServiceBeginDate,
                            ServiceEndDate = serviceItem.ServiceEndDate,
                            _ext = new EdFiExtension()
                            {
                                myBPS =
                            new Extension
                            {
                                serviceDuration = serviceItem._ext.myBPS.serviceDuration,
                                serviceDurationFrequency = serviceItem._ext.myBPS.serviceDurationFrequency,
                                serviceDurationRecurrenceDescriptor = "uri://mybps.org/ServiceDurationRecurrenceDescriptor#" + Constants.GetSDRecurrenceDesc(serviceItem._ext.myBPS.serviceDurationRecurrenceDescriptor),
                                serviceDurationUnitDescriptor = "uri://mybps.org/ServiceDurationUnitDescriptor#" + Constants.GetSDUnitDesc(serviceItem._ext.myBPS.serviceDurationUnitDescriptor),
                                serviceLocation = serviceItem._ext.myBPS.serviceLocation,
                                serviceClass = serviceItem._ext.myBPS.serviceClass
                            }
                            }
                        };
                        specialEducation.specialEducationProgramServices.Add(relatedService);
                    }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
            return specialEducation;


        }

        public List<EdFiStudentSpecialEducation> GetStudentSpecialEducation_IEP(string token, StudentSpecialEducationProgramAssociation spEducation)
        {
            
            List < EdFiStudentSpecialEducation > results = new List<EdFiStudentSpecialEducation>();
            var policy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(2, 0, 0) };
            IRestResponse response = null;
            var client1 = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "?studentUniqueId=" + spEducation.studentUniqueId);
            response = edfiApi.GetData(client1, token);

            if (IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length > 2)
                {
                    var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                    foreach (var item in data)
                    {
                        results.Add(item);
                        
                    }
                }

            }
            return results;
            
        }

        private static void InsertIEPStudentSpecialEducation(string token, StudentSpecialEducationProgramAssociation spEducation)
        {
            try
            {
                IRestResponse response = null;
                var rootObject = GetSpecialEducation(token, spEducation);                

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "?studentUniqueId=" + spEducation.studentUniqueId+Constants.programName + spEducation.programName);
                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                        foreach (var item in data)
                        {
                            string stuId = item.studentReference.studentUniqueId;
                            bool flag = false;

                            // Getting the DataSource as xml or txt for pcg records
                            //string dataSource = null;
                            //if (item._ext != null) dataSource = item._ext.myBPS.dataSource;
                            //rootObject._ext.myBPS.dataSource = Constants.SetDataSource(dataSource, rootObject._ext.myBPS.dataSource);

                            // Comparing the SourceSystemId in ODS and file
                            string sysSrcId_ODS = item._ext.myBPS.sourceSystemId;
                            string sysSrcId_file = spEducation.iepUniqueId;
                            if (sysSrcId_ODS != null && sysSrcId_file != null)
                                if (sysSrcId_ODS.Equals(sysSrcId_file)) flag = true;

                            if (item.id != null)
                            {
                                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                                if (!string.IsNullOrEmpty(spEducation.beginDate))
                                    if (stuId != null)
                                    {
                                        if (spEducation.programEducationOrganizationId == "" || spEducation.programEducationOrganizationId.Equals(null)) spEducation.programEducationOrganizationId = Constants.educationOrganizationIdValue;
                                        if (stuId == spEducation.studentUniqueId && flag == true)
                                        {
                                            //In case of different primary fields delete and create record
                                            if (!item.beginDate.Equals(spEducation.beginDate) || !item.programReference.educationOrganizationId.Equals(spEducation.programEducationOrganizationId.TrimStart('0')) || !item.programReference.ProgramName.Equals(spEducation.programName))
                                            {
                                                response = edfiApi.DeleteData(new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + item.id), token);
                                                if (IsSuccessStatusCode((int)response.StatusCode)) response = edfiApi.PostData(json, client, token);
                                                Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                                            }

                                        }
                                        //changes related to new sysSrcId, in case it is new Insert else for same primary fields to update
                                        else if (!sysSrcId_ODS.Equals(sysSrcId_file)) 
                                        {       
                                                response = edfiApi.PostData(json, client, token);
                                                Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                                                if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                                                {
                                                    LogError(item, response);

                                                }
                                        }

                                    }
                                    
                            }
                            

                            }
                        }
                    else
                    {
                        //rootObject._ext.myBPS.dataSource = Constants.DataSourceXml;
                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = edfiApi.PostData(json, client, token);
                        Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                        if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                        {
                            //LogError(item, response);

                        }
                    }
                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
        }
        private static void UpdateIEPStudentSpecialEducation(string token, StudentSpecialEducationProgramAssociation spEducation)
        {
            try
            {
                IRestResponse response = null;                
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "?studentUniqueId=" + spEducation.studentUniqueId+Constants.programType+spEducation.programTypeDescriptorId);
                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        
                        var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                        foreach (var item in data)
                        {
                            if(!item.programReference.ProgramName.Equals(Constants.programName504PlanValue))
                            {
                                var id = item.id;                                
                                
                                if (id != null)
                                {
                                    //updating the iepExitDate in case of populated exitdate in file
                                    item._ext.myBPS.iepExitDate = spEducation.iepExitDate;
                                    string json = JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented);
                                    if (item._ext != null)
                                    {
                                        if (!string.IsNullOrEmpty(item._ext.myBPS.iepExitDate))
                                            response = edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                    }
                                   
                                    if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                                    {
                                        LogError(item, response);

                                    }

                                }
                            }
                        }
                    }                   
                }

            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
        }

        private static void UpdateAlertStudentSpecialEducation(string token, SpecialEducation spEducation)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "?studentUniqueId=" + spEducation.StudentUniqueId);
                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                        foreach (var item in data)
                        {
                            if (item.programReference.ProgramName.Equals(Constants.programName504PlanValue))
                            {
                                var id = item.id;
                                string stuId = item.studentReference.studentUniqueId;

                                if (id != null)
                                {
                                    //retaining the values and updating iepExit date to ODS  
                                    item._ext.myBPS.iepExitDate = spEducation.IepExitDate;                                    
                                    string json = JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented);
                                    if (item._ext != null)
                                    {
                                        if (!string.IsNullOrEmpty(item._ext.myBPS.iepExitDate))
                                            response = edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                    }
                                    if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                                    {
                                        LogError(item, response);

                                    }


                                }


                            }
                        }



                    }

                }

            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
        }

        private static EdFiStudentSpecialEducation GetAlertSpecialEducation(string token, SpecialEducation spList)
        {
            EdFiStudentSpecialEducation specialEducation = new EdFiStudentSpecialEducation();
            try
            {

                specialEducation = new EdFiStudentSpecialEducation
                {
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    programReference = new ProgramReference
                    {
                        educationOrganizationId = spList.ProgramEducationOrganizationId,
                        programTypeDescriptor = "uri://ed-fi.org/ProgramTypeDescriptor#" + spList.Type,
                        ProgramName = spList.Name,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    studentReference = new StudentReference
                    {
                        studentUniqueId = spList.StudentUniqueId,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    beginDate = spList.BeginDate,
                    ideaEligibility = spList.IdeaEligibility,
                    iepReviewDate = spList.IepReviewDate,
                    iepBeginDate = spList.IepBeginDate,
                    iepEndDate = spList.IepEndDate,
                    specialEducationProgramServices = new List<Service>(),
                    _ext = new EdFiExt()
                    {
                        myBPS = new Ext()
                        {
                            iepExitDate = spList.IepExitDate,
                            parentResponse = spList.IepParentResponse,
                            //dataSource = Constants.DataSourceXml

                        }
                    }



                };

                if (spList.ServiceDescriptor != null)
                {
                    PostServiceDescriptor(spList.ServiceDescriptor, token);
                    var transportationCodeService = new Service
                    {
                        PrimaryIndicator = false, // default is false
                        SpecialEducationProgramServiceDescriptor = "uri://mybps.org/SpecialEducationProgramServiceDescriptor#" + $"{string.Concat(spList.ServiceDescriptor.ToString().Trim().Take(50))}",
                        ServiceBeginDate = spList.IepBeginDate,
                        ServiceEndDate = spList.IepEndDate,
                        _ext = new EdFiExtension()
                        {
                            myBPS =
                            new Extension
                            {
                                serviceDuration = "0",
                                serviceDurationFrequency = "0",
                                serviceDurationRecurrenceDescriptor = "uri://mybps.org/ServiceDurationRecurrenceDescriptor#" + Constants.GetSDRecurrenceDesc(null),
                                serviceDurationUnitDescriptor = "uri://mybps.org/ServiceDurationUnitDescriptor#" + Constants.GetSDUnitDesc(null),

                            }
                        }
                    };
                    specialEducation.specialEducationProgramServices.Add(transportationCodeService);

                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
            return specialEducation;
        }

        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private SpecialEducation GetAlertXml(XmlNode node)
        {
            try
            {
                string ServiceDesc = null;
                SpecialEducation studentEducationRefList = null;
                XmlNode ProgramNode = node.SelectSingleNode("programReference");
                XmlNode studentNode = node.SelectSingleNode("studentReference");
                XmlNode serviceDesc = node.SelectSingleNode("service");
                if (serviceDesc == null) ServiceDesc = null;
                else ServiceDesc = serviceDesc.SelectSingleNode("serviceDescriptor").InnerText;
                if (ProgramNode != null && studentNode != null)
                {
                    studentEducationRefList = new SpecialEducation
                    {
                        ProgramEducationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText?? null,
                        Type = ProgramNode.SelectSingleNode("type").InnerText ?? null,
                        Name = ProgramNode.SelectSingleNode("name").InnerText ?? null,
                        StudentUniqueId = studentNode.SelectSingleNode("studentUniqueId").InnerText ?? null,
                        ServiceDescriptor = ServiceDesc,
                        BeginDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null,
                        EndDate = node.SelectSingleNode("endDate").InnerText ?? null,
                        IdeaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false,
                        IepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null,
                        IepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null,
                        IepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null,
                        IepParentResponse = node.SelectSingleNode("iepParentResponse").InnerText ?? null,
                        IepSignatureDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null,
                        Eligibility504 = node.SelectSingleNode("programEligibility").InnerText ?? null,                        
                        IepExitDate = node.SelectSingleNode("Section504ExitDate").InnerText.ToString() ?? null
                        
                };
                }
                if (String.IsNullOrEmpty(studentEducationRefList.EducationOrganizationId))
                    studentEducationRefList.EducationOrganizationId = Constants.educationOrganizationIdValue;
                return studentEducationRefList;
            }

            catch (Exception ex)
            {
                Log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
        }

        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private static StudentSpecialEducationProgramAssociation GetSpecialEducationXml(XmlNode node)
        {
            try
            {
                StudentSpecialEducationProgramAssociation spEducation = new StudentSpecialEducationProgramAssociation();
                spEducation.relatedServices = new List<Service>();
                if(!String.IsNullOrEmpty(node.SelectSingleNode("iepUniqueId").InnerText))
                    spEducation.iepUniqueId = node.SelectSingleNode("iepUniqueId").InnerText.ToString()?? null;
                XmlNode EducationOrgNode = node.SelectSingleNode("educationOrganizationReference");
                if (EducationOrgNode != null)
                {
                    spEducation.educationOrganizationId = EducationOrgNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                }
                    
                XmlNode ProgramNode = node.SelectSingleNode("programReference");
                if (ProgramNode != null)
                {
                    spEducation.programEducationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                    if (String.IsNullOrEmpty(spEducation.programEducationOrganizationId))
                        spEducation.programEducationOrganizationId = Constants.educationOrganizationIdValue;
                    spEducation.programTypeDescriptorId =  ProgramNode.SelectSingleNode("type").InnerText.ToString() ?? null;
                    spEducation.programName = ProgramNode.SelectSingleNode("name").InnerText.ToString() ?? null;
                    if (String.IsNullOrEmpty(spEducation.programName )) spEducation.programName = Constants.ProgramName;
                }
                XmlNode studentNode = node.SelectSingleNode("studentReference");
                if (studentNode != null)
                {
                    spEducation.studentUniqueId = studentNode.SelectSingleNode("studentUniqueId").InnerText.ToString() ?? null;
                    
                }                
                //spEducation.ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.ToString();
                spEducation.iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText.ToString() ?? null;
                spEducation.iepEndDate = node.SelectSingleNode("iepEndDate").InnerText.ToString() ?? null;
                spEducation.iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText.ToString() ?? null;
                spEducation.iepExitDate = node.SelectSingleNode("exitDate").InnerText.ToString() ?? null;
                spEducation.lastEvaluationDate = node.SelectSingleNode("lastEvaluationDate").InnerText.ToString() ?? null;
                spEducation.parentResponse = node.SelectSingleNode("parentResponse").InnerText ?? null;
                //Exit Date : logic needs to be implemented for IEP records already ended
                string dateSigned = node.SelectSingleNode("dateSigned").InnerText ?? null;
                if (!string.IsNullOrEmpty(dateSigned) && !string.IsNullOrEmpty(spEducation.iepBeginDate))
                {
                    if (DateTime.Parse(dateSigned, CultureInfo.InvariantCulture) >= DateTime.Parse(spEducation.iepBeginDate, CultureInfo.InvariantCulture))
                        spEducation.beginDate = dateSigned;
                    else spEducation.beginDate = spEducation.iepBeginDate;
                }
                else
                {
                    if (!string.IsNullOrEmpty(dateSigned)) spEducation.beginDate = dateSigned;
                    else spEducation.beginDate = spEducation.iepBeginDate;
                }
                                             
                //if (string.IsNullOrEmpty(beginDate))beginDate = spEducation.iepBeginDate ;                
                //spEducation.beginDate = beginDate;
                var agencyNode = node.SelectSingleNode("Agency");
                if (agencyNode != null)
                spEducation.costSharingAgency = node.SelectSingleNode("Agency").InnerText.ToString() ?? null;
                var costShareNode = node.SelectSingleNode("CostShare");
                if (costShareNode != null)
                {
                    string costShare = node.SelectSingleNode("CostShare").InnerText.ToString() ?? null;
                    if (!string.IsNullOrEmpty(costShare))
                    {
                        if (costShare == "No") spEducation.isCostSharing = false;
                        else spEducation.isCostSharing = true;

                    }
                }
                //string dataSource = null;
                //if (node.SelectSingleNode("DataSource")!= null)
                //    dataSource = node.SelectSingleNode("DataSource").InnerText.ToString();
                //spEducation.dataSource = Constants.GetDataSource(dataSource);

                spEducation.medicallyFragile = false;
                //spEducation.multiplyDisabled = false;
                if (node.SelectSingleNode("schoolHoursPerWeek")!= null)if (node.SelectSingleNode("schoolHoursPerWeek").InnerText.Trim().Length > 0)                    
                    spEducation.schoolHoursPerWeek = Convert.ToDecimal(node.SelectSingleNode("schoolHoursPerWeek").InnerText.ToString()); // Null Check req need to Modify
                if (node.SelectSingleNode("specialEducationHoursPerWeek") != null) if (node.SelectSingleNode("specialEducationHoursPerWeek").InnerText.Trim().Length > 0)
                        if (Convert.ToDecimal(node.SelectSingleNode("specialEducationHoursPerWeek").InnerText.ToString()) < Convert.ToDecimal(999.99))
                            spEducation.specialEducationHoursPerWeek = Convert.ToDecimal(node.SelectSingleNode("specialEducationHoursPerWeek").InnerText.ToString()); // Null Check req need to Modify
                        else spEducation.specialEducationHoursPerWeek = Convert.ToDecimal(Constants.OutofBoundValue);
                if (node.SelectSingleNode("SpecialEducationSetting") != null) if (node.SelectSingleNode("SpecialEducationSetting").InnerText.Trim().Length > 0)
                    spEducation.specialEducationSettingDescriptorId = Constants.GetSpecialEducationSetting(Int32.Parse(node.SelectSingleNode("SpecialEducationSetting").InnerText.ToString())) ?? null;
                        
                XmlNodeList serviceDescriptor = node.SelectNodes("service");                
                foreach (XmlElement nvNode in serviceDescriptor)
                {
                    var relatedService = new Service
                    {
                    SpecialEducationProgramServiceDescriptor = nvNode.SelectSingleNode("serviceDescriptor").InnerText.ToString()?? null,
                    PrimaryIndicator = true,
                    ServiceBeginDate = nvNode.SelectSingleNode("serviceBeginDate").InnerText.ToString() ?? null,
                    ServiceEndDate = nvNode.SelectSingleNode("serviceEndDate").InnerText.ToString() ?? null,
                    _ext = new EdFiExtension
                         {
                        myBPS = new Extension
                            {
                                serviceClass = nvNode.SelectSingleNode("serviceClass").InnerText.ToString() ?? null,
                                serviceLocation = nvNode.SelectSingleNode("serviceLocation").InnerText.ToString() ?? null,
                                serviceDuration = nvNode.SelectSingleNode("serviceDuration").InnerText.ToString() ?? null,
                                serviceDurationFrequency = nvNode.SelectSingleNode("serviceDurationFrequency").InnerText.ToString() ?? null,
                                serviceDurationRecurrenceDescriptor = nvNode.SelectSingleNode("serviceDurationPer").InnerText.ToString() ?? null,
                                serviceDurationUnitDescriptor = nvNode.SelectSingleNode("serviceDurationIn").InnerText.ToString() ?? null
                             

                            }
                         }
                    };
                    if (!String.IsNullOrEmpty(relatedService.SpecialEducationProgramServiceDescriptor))
                    if (String.IsNullOrEmpty(relatedService._ext.myBPS.serviceDuration))
                            relatedService._ext.myBPS.serviceDuration = "0.0000";
                    spEducation.relatedServices.Add(relatedService);

                }
                
                return spEducation;
            }

            catch (Exception ex)
            {
                Log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
        }

        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private static SpedSimsTxt GetStudentSpedXml(XmlNode node)
        {
            try
            {
                SpedSimsTxt studentDisabilityList = null;               
                                
                if (node != null)
                {
                    studentDisabilityList = new SpedSimsTxt
                    {
                        studentNumber = node.SelectSingleNode("studentNumber").InnerText ?? null,
                        disabilityInfo = node.SelectSingleNode("disabilityDesc").InnerText ?? null,
                        levelNeedInfo = node.SelectSingleNode("levelNeed").InnerText ?? null
                    };

                }
                return studentDisabilityList;
            }

            catch (Exception ex)
            {
                Log.Error("Error getting Emplyment data from importDisability xml : Exception : " + ex.Message);
                return null;

            }
        }
        private static string GetPercisedDecimal(string num)
        {
            if(num != null)
            {
                if(num.Length >5)num = num.Substring(0, 5);                
                num = decimal.Divide(Convert.ToDecimal(num), 100).ToString();
                
            }
            
            return num;
        }
        /// <summary>
        /// POST the Data from the BPS Interface View to EdFi ODS
        /// </summary>
        /// <returns></returns>
        private static bool PostServiceDescriptor(string Item, string token)
        {
            bool isPosted = true;
            List<ServiceDescriptor> serviceDescriptorList = new List<ServiceDescriptor>();
            IRestResponse response = null;


            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_ProgramServiceDescriptor);
            var rootObject = new ServiceDescriptor
            {
                CodeValue = string.Concat(Item.ToString().Trim().Take(50)),
                ShortDescription = string.Concat(Item.ToString().Trim().Take(75)),
                Description = Item.ToString().Trim(),
                EffectiveBeginDate = null,
                EffectiveEndDate = null,
                Namespace = "uri://mybps.org/SpecialEducationProgramServiceDescriptor",
                PriorDescriptorId = 0


            };
            string json = JsonConvert.SerializeObject(rootObject, Formatting.Indented);
            response = edfiApi.PostData(json, client, token);
            if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
            {
                //Log the Error
                Log.Error("Something went wrong while updating the Service Descriptor in ODS, check the XML values" + response.Content.ToString());

            }
            return isPosted;

        }
        private static void LogError(EdFiStudentSpecialEducation item,IRestResponse response )
        {
            ErrorLog errorLog = new ErrorLog();
            errorLog.StudentLocalID = item.studentReference.studentUniqueId.Trim() ?? null;
            errorLog.EducationOrganizationId = item.programReference.educationOrganizationId ?? null;
            errorLog.Type = item.programReference.programTypeDescriptor ?? null;
            errorLog.Name = item.programReference.ProgramName ?? null;
            errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
            ErrorLogging(errorLog);
        }

        private static void ErrorLogging(ErrorLog errorLog)
        {
            string strPath = Constants.LOG_FILE;
            bool isFirst = false;
            if (!File.Exists(strPath))
            {
                File.Create(strPath).Dispose();
                isFirst = true;
            }
            using (StreamWriter sw = File.AppendText(strPath))
            {
                if (isFirst)
                sw.WriteLine("StudentId,EducationOrganizationId,ProgramTypeID,ProgramName,ErrorMessage");
                sw.WriteLine("{0},{1},{2},{3},{4}", errorLog.StudentLocalID, errorLog.EducationOrganizationId, errorLog.Type, errorLog.Name, errorLog.ErrorMessage);
                sw.Close();
            }


        }

    }
}
