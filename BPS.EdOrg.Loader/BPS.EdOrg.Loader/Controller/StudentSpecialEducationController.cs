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
using System.Web;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BPS.EdOrg.Loader.Controller
{
    class StudentSpecialEducationController
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private Notification notification;
        public void UpdateAlertSpecialEducationData(string token, ParseXmls prseXMl)
        {

            try
            {
                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"/504inXML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
                fragments = fragments.Replace("504Eligibility", "_504Eligibility");
                var myRootedXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><roots>" + fragments + "</roots>";
                var doc = XDocument.Parse(myRootedXml);
                XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//roots/root");
                foreach (XmlNode node in nodeList)
                {
                    var studentSpecialEducationList = GetAlertXml(node);

                    if (studentSpecialEducationList.EducationOrganizationId != null && studentSpecialEducationList.Name != null && studentSpecialEducationList.Type != null)
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, studentSpecialEducationList.EducationOrganizationId, studentSpecialEducationList.Name, studentSpecialEducationList.Type);
                        if (studentSpecialEducationList.StudentUniqueId != null)
                        {
                            InsertAlertDataSpecialEducation(token, studentSpecialEducationList);
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
        public void UpdateEndDateSpecialEducation(string token, ParseXmls prseXMl)
        {
            try {
                int offset = 0;
                List<SpecialEducationReference> studentSpecialEducations = null;
                studentSpecialEducations = GetStudentSpecialEducation(token, offset);
                while (studentSpecialEducations != null && studentSpecialEducations.Any())
                {
                    foreach (var item in studentSpecialEducations)
                    {
                        var endDate = GetEndDateProgramAssociation(token, item);
                        item.endDate = endDate;
                        SetEndDate(token, item);
                    }

                    offset = offset + 100;
                    studentSpecialEducations = GetStudentSpecialEducation(token, offset);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }


       


        public List<SpecialEducationReference> GetStudentSpecialEducation(string token,int offset = 0)
        {
            List<SpecialEducationReference> data =  null;
            try
            {
                if (token != null)
                {
                    var client = offset == 0 ? new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit+ Constants.program504Plan)
                                           : new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit+ Constants.program504Plan + "&offset=" + offset);
                    var response = edfiApi.GetData(client, token);

                    if (IsSuccessStatusCode((int)response.StatusCode))
                    {
                        //formattedResponse = JArray.Parse(response.Content.ToString());
                         data = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);                
            }
            return data;
        }
        public void UpdateIEPSpecialEducationProgramAssociationData(string token, ParseXmls prseXMl)
        {
            try
            {
                if (!Directory.Exists(ConfigurationManager.AppSettings["XMLExtractedPath"]))
                    Directory.CreateDirectory(ConfigurationManager.AppSettings["XMLExtractedPath"]);
                string[] filePaths = Directory.GetFiles(ConfigurationManager.AppSettings["XMLExtractedPath"]);
                foreach (System.IO.FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                    file.Delete();
                ZipFile.ExtractToDirectory(ConfigurationManager.AppSettings["XMLDeploymentPath"] + ConfigurationManager.AppSettings["XMLZip"], ConfigurationManager.AppSettings["XMLExtractedPath"]);
                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    var fragments = File.ReadAllText(file.FullName).Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
                    var doc = XDocument.Parse(fragments);
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");
                    foreach (XmlNode node in nodeList)
                    {
                        // Null Check req need to Modify
                        var spEducation = GetSpecialEducationXml(node);                                                                                                                                                                              //Check required fied exist in XML source 
                        if (!string.IsNullOrEmpty(spEducation.programReference.EducationOrganizationId) && !string.IsNullOrEmpty(spEducation.programReference.ProgramName) && !string.IsNullOrEmpty(spEducation.programReference.ProgramTypeDescriptor) && !string.IsNullOrEmpty(spEducation.beginDate) && !string.IsNullOrEmpty(spEducation.studentReference.studentUniqueId)) // 
                        {
                            // Check if the Program already exists in the ODS if not first enter the Progam.
                            VerifyProgramData(token, spEducation.programReference.EducationOrganizationId, spEducation.programReference.ProgramName, spEducation.programReference.ProgramTypeDescriptor);
                            InsertIEPStudentSpecialEducation(token, spEducation);
                        }
                        else
                        {
                            Log.Info("Required fields are empty for studentUniqueId:" + spEducation.studentReference.studentUniqueId);
                        }
                    }
                    if (File.Exists(Constants.LOG_FILE))
                        notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public static IRestResponse VerifyProgramData(string token, string educationOrganizationId, string programName, string programType)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program + Constants.educationOrganizationId + educationOrganizationId + Constants.programName + programName + Constants.programType + programType);

                response = edfiApi.GetData(client, token);
                if (!IsSuccessStatusCode((int)response.StatusCode))
                {
                    var rootObject = new EdFiProgram
                    {
                        EducationOrganizationReference = new EdFiEducationReference
                        {
                            educationOrganizationId = educationOrganizationId,
                            Link = new Link()
                            {
                                Rel = string.Empty,
                                Href = string.Empty
                            }
                        },
                        ProgramId = null,
                        ProgramTypeDescriptor = programType,
                        SponsorType = string.Empty,
                        ProgramName = programName,


                    };

                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    response = edfiApi.PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program), token); // Need to Check - Get requred here 
                    Log.Info("Check if the program data exists in EdfI Program for programTypeId : " + programType);
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
        
        private static string GetEndDateProgramAssociation(string token, SpecialEducationReference spList)
        {
            string endDate = DateTime.Now.ToString();
            try
            {
                
                IRestResponse response = null;                 
                var client =new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation+ Constants.studentUniqueId + spList.studentReference.studentUniqueId+ Constants.program504Plan);
                response = edfiApi.GetData(client, token);
                if (response.Content.Length > 2)
                {
                    List<SpecialEducationReference> data = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                    if (data.Count() >= 2)
                    {
                        DateTime beginDate;
                        DateTime.TryParse(spList.beginDate, out beginDate);
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


        private static void SetEndDate(string token, SpecialEducationReference spItem)
        {           
            try
            {    IRestResponse response = null;                
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + spItem.studentReference.studentUniqueId + Constants.program504Plan+ Constants.beginDate+ spItem.beginDate);
                response = edfiApi.GetData(client, token);
                //dynamic original = JsonConvert.DeserializeObject(response.Content);
                List<SpecialEducationReference> original = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                if (response.Content.Length > 2)
                {
                    foreach (var data in original)
                    {
                        var rootObject = new SpecialEducationReference
                        {

                            endDate = spItem.endDate,
                            iepEndDate = data.iepEndDate,
                            lastEvaluationDate = data.lastEvaluationDate,
                            iepReviewDate = data.iepReviewDate,
                            iepBeginDate = data.iepBeginDate

                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
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

        private static string GetEndDateProgramAssociation1(string token, SpecialEducation spList)
        {
            string endDate = spList.IepSignatureDate;
            try
            {

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + spList.StudentUniqueId + Constants.program504Plan);
                response = edfiApi.GetData(client, token);
                if (response.Content.Length > 2)
                {
                    List<SpecialEducationReference> data = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);
                    if (data.Count() >= 2)
                    {
                        DateTime beginDate;
                        DateTime.TryParse(spList.IepSignatureDate, out beginDate);
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
                            endDate = spList.BeginDate;
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


        private static void InsertAlertDataSpecialEducation(string token, SpecialEducation spList)
        {
            try
            {
                IRestResponse response = null;
                var rootObject = new SpecialEducationReference
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
                        EducationOrganizationId = spList.EducationOrganizationId,
                        ProgramTypeDescriptor = spList.Type,
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
                    beginDate = spList.IepSignatureDate,
                    ideaEligibility = spList.IdeaEligibility,
                    iepReviewDate = spList.IepReviewDate,
                    iepBeginDate = spList.IepBeginDate,
                    iepEndDate = spList.IepEndDate,                    
                    Services = new List<Service>()
                };

                if (spList.ServiceDescriptor != null)
                {
                    PostServiceDescriptor(Constants.GetTransportationEligibility(spList.ServiceDescriptor), token);
                    var transportationCodeService = new Service
                    {
                        PrimaryIndicator = false, // default is false
                        ServiceDescriptor = $"{Constants.GetTransportationEligibility(spList.ServiceDescriptor)}",
                        ServiceBeginDate = spList.IepBeginDate,
                        ServiceEndDate = spList.IepEndDate
                    };
                    rootObject.Services.Add(transportationCodeService);

                }
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);

               // check for Student and BeginDate 
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + spList.StudentUniqueId + Constants.beginDate + spList.IepSignatureDate);
                response = edfiApi.GetData(client, token);
                dynamic original = JsonConvert.DeserializeObject(response.Content);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        
                        foreach (var data in original)
                        {
                            var id = data.id;
                            string stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate ;
                            DateTime.TryParse(data.beginDate.ToString("dd/MM/yyyy"), out iepDate) ;                        
                                

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
                                                response = edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                            else
                                                response = edfiApi.PostData(json, client, token);

                                        }
                                    }

                                }

                            }
                        }
                    }

                    else
                        response = edfiApi.PostData(json, client, token);                    
                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }


        }

        private static void InsertIEPStudentSpecialEducation(string token, SpecialEducationReference spEducation)
        {
            try
            {
                IRestResponse response = null;
                string json = JsonConvert.SerializeObject(spEducation, Newtonsoft.Json.Formatting.Indented);
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.SpecEduBeginDate + spEducation.beginDate + Constants.
                    SpecEduEducationOrganizationId + spEducation.educationOrganizationReference.educationOrganizationId + Constants.programEducationOrganizationId + spEducation.programReference.EducationOrganizationId +
                    Constants.SpecEduProgramName + spEducation.programReference.ProgramName + Constants.SpecEduProgramType + spEducation.programReference.ProgramTypeDescriptor + Constants.SpecEduStudentUniqueId + spEducation.studentReference.studentUniqueId);
                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        SpecialEducationReference data = JsonConvert.DeserializeObject<SpecialEducationReference>(response.Content);
                        var id = data.id;
                        string stuId = data.studentReference.studentUniqueId;
                        DateTime iepDate = Convert.ToDateTime(data.iepSignatureDate);
                        if (id != null)
                        {
                            if (spEducation.iepBeginDate != null)
                            {
                                if (stuId != null && iepDate != null)
                                {
                                    DateTime inputDateTime;
                                    if (DateTime.TryParse(spEducation.iepBeginDate, out inputDateTime))
                                    {
                                        var result = DateTime.Compare(inputDateTime, iepDate);
                                        if (stuId == spEducation.studentReference.studentUniqueId && result == 0)
                                            response = edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                        else
                                            response = edfiApi.PostData(json, client, token);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    response = edfiApi.PostData(json, client, token);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
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
                        EducationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null,
                        Type = ProgramNode.SelectSingleNode("type").InnerText ?? null,
                        Name = ProgramNode.SelectSingleNode("name").InnerText ?? null,
                        StudentUniqueId = studentNode.SelectSingleNode("studentUniqueId").InnerText ?? null,
                        ServiceDescriptor = ServiceDesc,
                        BeginDate = node.SelectSingleNode("beginDate").InnerText ?? null,
                        EndDate = node.SelectSingleNode("endDate").InnerText ?? null,
                        IdeaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false,
                        IepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null,
                        IepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null,
                        IepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null,
                        IepParentResponse = node.SelectSingleNode("iepParentResponse").InnerText ?? null,
                        IepSignatureDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null,
                        Eligibility504 = node.SelectSingleNode("_504Eligibility").InnerText ?? null

                    };
                }
                 
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
        private SpecialEducationReference GetSpecialEducationXml(XmlNode node)
        {
            try
            {
                SpecialEducationReference spEducation = new SpecialEducationReference();
                spEducation.educationOrganizationReference = new EdFiEducationReference();
                spEducation.studentReference = new StudentReference();
                spEducation.programReference = new ProgramReference();

                spEducation.educationOrganizationReference.educationOrganizationId = Constants.educationOrganizationIdValue;
                spEducation.educationOrganizationReference.Link = new Link()
                {
                    Rel = string.Empty,
                    Href = string.Empty
                };
                XmlNode ProgramNode = node.SelectSingleNode("programReference");
                if (ProgramNode != null)
                {
                    spEducation.programReference.EducationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                    spEducation.programReference.ProgramTypeDescriptor = ProgramNode.SelectSingleNode("type").InnerText.ToString() ?? null;
                    spEducation.programReference.ProgramName = ProgramNode.SelectSingleNode("name").InnerText.ToString() ?? null;
                    spEducation.programReference.Link = new Link()
                    {
                        Rel = string.Empty,
                        Href = string.Empty
                    };
                }
                XmlNode studentNode = node.SelectSingleNode("studentReference");
                if (studentNode != null)
                {
                    spEducation.studentReference.studentUniqueId = studentNode.SelectSingleNode("studentUniqueId").InnerText.ToString() ?? null;
                    spEducation.studentReference.Link = new Link()
                    {
                        Rel = string.Empty,
                        Href = string.Empty
                    };
                }
                string beginDate = node.SelectSingleNode("beginDate").InnerText ?? null;
                if (string.IsNullOrEmpty(beginDate))
                    beginDate = ConfigurationManager.AppSettings["SchoolStartDate"];
                spEducation.beginDate = beginDate;
                spEducation.endDate = node.SelectSingleNode("endDate").InnerText ?? null;
                //spEducation.ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false;
                spEducation.iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText.ToString() ?? null;
                spEducation.iepEndDate = node.SelectSingleNode("iepEndDate").InnerText.ToString() ?? null;
                spEducation.iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText.ToString() ?? null;
                spEducation.lastEvaluationDate = node.SelectSingleNode("lastEvaluationDate").InnerText.ToString() ?? null;
                //spEducation.medicallyFragile = null;
                //spEducation.multiplyDisabled = null;
                spEducation.reasonExitedDescriptor = node.SelectSingleNode("reasonExitedDescriptor").InnerText.ToString() ?? null;
                if (node.SelectSingleNode("schoolHoursPerWeek").InnerText.Trim().Length > 0)
                    spEducation.schoolHoursPerWeek = Convert.ToDouble(node.SelectSingleNode("schoolHoursPerWeek").InnerText.ToString()); // Null Check req need to Modify
                if (node.SelectSingleNode("specialEducationHoursPerWeek").InnerText.Trim().Length > 0)
                    spEducation.specialEducationHoursPerWeek = Convert.ToDouble(node.SelectSingleNode("specialEducationHoursPerWeek").InnerText.ToString()); // Null Check req need to Modify
                if (node.SelectSingleNode("SpecialEducationSetting").InnerText.Trim().Length > 0)
                    spEducation.specialEducationSettingDescriptor = Constants.GetSpecialEducationSetting(Int32.Parse(node.SelectSingleNode("SpecialEducationSetting").InnerText.ToString()));

                return spEducation;
            }

            catch (Exception ex)
            {
                Log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
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


            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_ServiceDescriptor);
            var rootObject = new ServiceDescriptor
            {
                CodeValue = string.Concat(Item.ToString().Trim().Take(50)),
                ShortDescription = string.Concat(Item.ToString().Trim().Take(75)),
                Description = Item.ToString().Trim(),
                EffectiveBeginDate = null,
                EffectiveEndDate = null,
                //Namespace = "http://ed-fi.org/Descriptor/BPS/SPED/ServiceDescriptor.xml",
                Namespace = "http://ed-fi.org/Descriptor/BPS/SPEDTransportation/ServiceDescriptor.xml",
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

    }
}
