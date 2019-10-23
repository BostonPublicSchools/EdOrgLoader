﻿using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using BPS.EdOrg.Loader.ApiClient;
using Newtonsoft.Json;
using RestSharp;
using EdFi.OdsApi.Sdk;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Linq;
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.XMLDataLoad;
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;


namespace BPS.EdOrg.Loader.Controller
{
    class StudentSpecialEducationController
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private Notification notification = new Notification();
        public void UpdateAlertSpecialEducationData(string token, ParseXmls prseXMl)
        {

            try
            {
                string typeValue = null;
                string nameValue = null;
                string educationOrganizationIdValue = null;
                string studentUniqueIdValue = null;


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
                    List<SpecialEducationReference> spEducationList = new List<SpecialEducationReference>();

                    XmlNode ProgramNode = node.SelectSingleNode("programReference");
                    if (ProgramNode != null)
                    {
                        educationOrganizationIdValue = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                        typeValue = ProgramNode.SelectSingleNode("type").InnerText ?? null;
                        nameValue = ProgramNode.SelectSingleNode("name").InnerText ?? null;

                    }

                    XmlNode studentNode = node.SelectSingleNode("studentReference");
                    if (studentNode != null)
                    {
                        studentUniqueIdValue = studentNode.SelectSingleNode("studentUniqueId").InnerText ?? null;

                    }

                    string beginDate = node.SelectSingleNode("beginDate").InnerText ?? null;
                    string endDate = node.SelectSingleNode("endDate").InnerText ?? null;
                    bool ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false;
                    string iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null;
                    string iepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null;
                    string iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null;
                    string iepParentResponse = node.SelectSingleNode("iepParentResponse").InnerText ?? null;
                    string iepSignatureDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null;
                    string Eligibility504 = node.SelectSingleNode("_504Eligibility").InnerText ?? null;

                    if (educationOrganizationIdValue != null && nameValue != null && typeValue != null)
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, educationOrganizationIdValue, nameValue, typeValue);
                        if (studentUniqueIdValue != null)
                            InsertAlertDataSpecialEducation(token, typeValue, nameValue, educationOrganizationIdValue, studentUniqueIdValue, beginDate, endDate, ideaEligibility, iepBeginDate, iepEndDate, iepReviewDate, iepParentResponse, iepSignatureDate, Eligibility504);

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
        public void UpdateIEPSpecialEducationProgramAssociationData(string token, ParseXmls prseXMl)
        {
            try
            {
                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"Aspen_in_XML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
                var doc = XDocument.Parse(fragments);
                XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);
                XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");
                foreach (XmlNode node in nodeList)
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
                        spEducation.programReference.educationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                        spEducation.programReference.type = ProgramNode.SelectSingleNode("type").InnerText.ToString() ?? null;
                        spEducation.programReference.name = ProgramNode.SelectSingleNode("name").InnerText.ToString() ?? null;
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
                        spEducation.specialEducationSettingDescriptor = Constants.getSpecialEducationSetting(Int32.Parse(node.SelectSingleNode("SpecialEducationSetting").InnerText.ToString())); // Null Check req need to Modify
                    //Check required fied exist in XML source 
                    if (!string.IsNullOrEmpty(spEducation.programReference.educationOrganizationId) && !string.IsNullOrEmpty(spEducation.programReference.name) && !string.IsNullOrEmpty(spEducation.programReference.type) && !string.IsNullOrEmpty(spEducation.beginDate) && !string.IsNullOrEmpty(spEducation.studentReference.studentUniqueId)) // 
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, spEducation.programReference.educationOrganizationId, spEducation.programReference.name, spEducation.programReference.type);
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
                        educationOrganizationReference = new EdFiEducationReference
                        {
                            educationOrganizationId = educationOrganizationId,
                            Link = new Link()
                            {
                                Rel = string.Empty,
                                Href = string.Empty
                            }
                        },
                        programId = null,
                        type = programType,
                        sponsorType = string.Empty,
                        name = programName,


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

        private static void InsertAlertDataSpecialEducation(string token, string type, string name, string educationId, string studentId, string beginDate, string endDate, bool ideaEligibility, string iepBeginDate, string iepEndDate, string iepReviewDate, string iepParentResponse, string iepSignatureDate, string Eligibility504)
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
                        educationOrganizationId = educationId,
                        type = type,
                        name = name,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    studentReference = new StudentReference
                    {
                        studentUniqueId = studentId,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    beginDate = iepBeginDate,
                    ideaEligibility = ideaEligibility,
                    iepReviewDate = iepReviewDate,
                    iepBeginDate = iepBeginDate,
                    iepEndDate = iepEndDate,
                    endDate = endDate,
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + studentId + Constants.beginDate + iepBeginDate);
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
                            DateTime iepDate = data.iepBeginDate;
                            if (id != null)
                            {
                                if (iepBeginDate != null)
                                {
                                    if (stuId != null && iepDate != null)
                                    {

                                        DateTime inputDateTime;
                                        if (DateTime.TryParse(iepBeginDate, out inputDateTime))
                                        {
                                            var result = DateTime.Compare(inputDateTime, iepDate);
                                            if (stuId == studentId && result == 0)
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
                    SpecEduEducationOrganizationId + spEducation.educationOrganizationReference.educationOrganizationId + Constants.programEducationOrganizationId + spEducation.programReference.educationOrganizationId +
                    Constants.SpecEduProgramName + spEducation.programReference.name + Constants.SpecEduProgramType + spEducation.programReference.type + Constants.SpecEduStudentUniqueId + spEducation.studentReference.studentUniqueId);
                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        SpecialEducationReference data = JsonConvert.DeserializeObject<SpecialEducationReference>(response.Content);
                        var id = data.id;
                        string stuId = data.studentReference.studentUniqueId;
                        DateTime iepDate = Convert.ToDateTime(data.iepBeginDate);
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
    }
}