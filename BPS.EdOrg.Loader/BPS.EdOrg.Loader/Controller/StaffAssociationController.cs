using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using BPS.EdOrg.Loader.ApiClient;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Linq;
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.XMLDataLoad;
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;


namespace BPS.EdOrg.Loader.Controller
{

    class StaffAssociationController
    {

        private RestServiceManager _restServiceManager;
        private Notification _notification;
        private readonly ILog _log;
        private ParseXmls _prseXML;
        private EdFiApiCrud _edfiApi ;
        public StaffAssociationController(string token, EdorgConfiguration configuration,ILog log)
        {
            _log = log;
            _prseXML = new ParseXmls(configuration, _log);
            _restServiceManager = new RestServiceManager(configuration, token, _log);           
            _edfiApi = new EdFiApiCrud();
        }
        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationEmploymentAssociation table.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffEmploymentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffAssociation");
                _restServiceManager = new RestServiceManager(configuration, token, _log);

                var existingStaffIds = _restServiceManager.GetStaffList();
                var schoolDeptids = GetDeptList(configuration);
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().OrderBy(element => element.SelectSingleNode("EmploymentPeriod/EndDate").InnerText).ToList();
                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffEmploymentNodeList = GetEmploymentAssociationXml(node);
                    if (staffEmploymentNodeList != null)
                    {
                        // Adding new staff from peoplesoft file.
                        if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.firstName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.lastName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.birthDate))
                        {
                            if (!existingStaffIds.Any(p => p == staffEmploymentNodeList.staffUniqueIdValue))
                                UpdatingNewStaffData(token, staffEmploymentNodeList.staffUniqueIdValue, staffEmploymentNodeList.staff.firstName, staffEmploymentNodeList.staff.middleName, staffEmploymentNodeList.staff.lastName, staffEmploymentNodeList.staff.birthDate);
                        }

                        // if status is terminated and there are more than one row then set endDate as null
                        if (staffEmploymentNodeList.status == "T")
                        {
                            int countStd = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/StaffReference/StaffIdentity/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == staffEmploymentNodeList.staffUniqueIdValue).Distinct().Count();
                            if (countStd > 1) staffEmploymentNodeList.endDateValue = null;
                        }

                        //Inserting data for CentralEmployees as '9035' to Employment Association
                        var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffEmploymentNodeList.educationOrganizationIdValue) && x.OperationalStatus.Equals("Active")).FirstOrDefault();
                        if (educationOrganizationId == null)
                        {
                            staffEmploymentNodeList.educationOrganizationIdValue = Constants.educationOrganizationIdValueCentralStaff;
                            GetEmploymentAssociationId(token, staffEmploymentNodeList);
                            UpdateStaffSchoolAssociation(token, Constants.educationOrganizationIdValueCentralStaff, staffEmploymentNodeList.empDesc, staffEmploymentNodeList.staffUniqueIdValue, Constants.StaffEmploymentUrl);
                        }

                        // updating the values in Employment Association 
                        if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.hireDateValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.empDesc))
                        {
                            staffEmploymentNodeList.educationOrganizationIdValue = Constants.educationOrganizationIdValue;
                            string id = GetEmploymentAssociationId(token, staffEmploymentNodeList);
                            if (id != null)
                            {                            
                                string endDate = GetAssignmentEndDate(token, staffEmploymentNodeList.staffUniqueIdValue, staffEmploymentNodeList.empDesc, null,Constants.StaffAssignmentUrl);
                                //Setting the Enddate with the one from AssignmentAssociation
                                    if (endDate != null)
                                        staffEmploymentNodeList.endDateValue = endDate;                                                                
                                UpdateEndDate(token, id, staffEmploymentNodeList);
                            }

                        }
                    }

                }
                if (File.Exists(Constants.LOG_FILE))
                {
                    _notification = new Notification(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
                }
                  
            }
            catch (Exception ex)
            {
                _log.Error("Error working on EmploymentAssociation Data : " + ex.Message);
            }
        }
 
        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private StaffEmploymentAssociationData GetEmploymentAssociationXml(XmlNode node)
        {
            try
            {
                StaffEmploymentAssociationData staffEmploymentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");
                if (staffNode != null && EmploymentNode != null)
                {
                    staffEmploymentList = new StaffEmploymentAssociationData
                    {

                        educationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        staffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        
                        staff = new StaffData
                        {
                            firstName = staffNode.SelectSingleNode("FirstName").InnerText ?? null,
                            middleName = staffNode.SelectSingleNode("MiddleName").InnerText ?? null,
                            lastName = staffNode.SelectSingleNode("LastName").InnerText ?? null,
                            birthDate = staffNode.SelectSingleNode("BirthDate").InnerText ?? null,
                        },
                        status = EmploymentNode.SelectSingleNode("Status").InnerText ?? null,
                        hireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        endDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        empDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null

                    };
                }
                return staffEmploymentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
        }

        //}
        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationEmploymentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private string GetEmploymentAssociationId(string token, StaffEmploymentAssociationData staffData)
        {
            IRestResponse response = null;
            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + Constants.educationOrganizationId + staffData.educationOrganizationIdValue + Constants.employmentStatusDescriptor + Uri.EscapeDataString(Constants.employmentStatusDescriptorValue) + staffData.empDesc + Constants.hireDate + staffData.hireDateValue + Constants.staffUniqueId + staffData.staffUniqueIdValue);
            response = _edfiApi.GetData(client, token);

            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length > 2)
                {
                    dynamic original = JsonConvert.DeserializeObject<StaffAssociationReference>(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }));
                    var id = original.id;
                    if (id != null)
                        return id;
                }
            }
            else
            {
                //Insert Data 
                var rootObject = new StaffEmploymentDescriptor
                {

                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = staffData.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    staffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffData.staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = Constants.employmentStatusDescriptorValue + staffData.empDesc,
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                _edfiApi.PostData(json, client, token);
                _log.Info("Inserted into StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
            }

            return null;
        }

        /// <summary>
        /// Gets the endDate from AsignmentAssociation table for a staff.
        /// </summary>
        /// <returns></returns>
        private string GetAssignmentEndDate(string token, string staffUniqueId, string empDesc, string schoolId, string url)
        {
            string endDate = null;
            DateTime maxValue = default(DateTime);
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + url + Constants.educationOrganizationId + schoolId + Constants.staffUniqueId + staffUniqueId + Constants.employmentStatusDescriptor + Uri.EscapeDataString(Constants.employmentStatusDescriptorValue) + empDesc);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var original = JsonConvert.DeserializeObject<List<UpdateEndDate>>(response.Content);
                        foreach (var data in original)
                        {
                            if (string.IsNullOrEmpty(data.EndDate))
                            {
                                endDate = null;
                                break;
                            }
                            DateTime inputDateTime;
                            DateTime.TryParse(data.EndDate, out inputDateTime);
                            int result = DateTime.Compare(inputDateTime, maxValue);
                            if (result >= 0)
                            {
                                maxValue = inputDateTime;
                                endDate = maxValue.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return endDate;
        }

        /// <summary>
        /// Updates the enddate to [StaffEducationOrganizationEmploymentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private void UpdateEndDate(string token, string id, StaffEmploymentAssociationData staffData)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + "/" + id);
                var rootObject = new StaffEmploymentDescriptor
                {
                    id = id,
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    staffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffData.staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = Constants.employmentStatusDescriptorValue + staffData.empDesc,
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue
                };

                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = _edfiApi.PutData(json, client, token);
                _log.Info("Updated StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
            }
            catch (Exception ex)
            {
                _log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
        }


        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationAssignmentAssociation table.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffAssignmentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffAssociation");
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");                
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().OrderBy(element => DateTime.Parse(element.SelectSingleNode("EmploymentPeriod/ActionDate").InnerText)).ToList();
                var schoolDeptids = GetDeptList(configuration);
                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffAssignmentNodeList = GetAssignmentAssociationXml(node);
                    if (staffAssignmentNodeList != null)
                    {
                        if (staffAssignmentNodeList.EducationOrganizationIdValue != null)
                        {
                            if (schoolDeptids.Count > 0)
                            {
                                string schoolid = null;
                                // Getting the EdOrgId for the Department ID 
                                var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue) && x.OperationalStatus.Equals("Active")).FirstOrDefault();

                                // setting the DeptId as EdOrgId for the staff, if no corresponding school is found
                                if (educationOrganizationId != null) schoolid = educationOrganizationId.SchoolId;
                                if (!string.IsNullOrEmpty(schoolid))
                                {
                                    //Inserting new Assignments and updating the postioTitle with JobCode - JobDesc
                                    if (!string.IsNullOrEmpty(staffAssignmentNodeList.StaffUniqueIdValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.BeginDateValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.StaffClassification) && !string.IsNullOrEmpty(staffAssignmentNodeList.PositionCodeDescription))
                                    {
                                        string id = GetAssignmentAssociationId(token, schoolid, staffAssignmentNodeList);
                                        if (id != null)
                                            UpdateAssignmentAssociation(token, id, schoolid, staffAssignmentNodeList);
                                    }
                                    //Update StaffSchoolAssociation for staff schools
                                    UpdateStaffSchoolAssociation(token, schoolid, staffAssignmentNodeList.EmpDesc, staffAssignmentNodeList.StaffUniqueIdValue,Constants.StaffAssignmentUrl);
                                }
                            }
                        }
                    }
                }
                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>         
        private StaffAssignmentAssociationData GetAssignmentAssociationXml(XmlNode node)
        {
            try
            {
                StaffAssignmentAssociationData staffAssignmentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                if (staffNode == null && EducationNode == null) _log.Error("Nodes not reurning any data for Assignment");
                XmlNode staffClassificationNode = node.SelectSingleNode("StaffClassification");
                XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");
                if (staffNode != null && EducationNode != null)
                {
                    staffAssignmentList = new StaffAssignmentAssociationData
                    {
                        StaffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        EducationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        EndDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        BeginDateValue = EmploymentNode.SelectSingleNode("BeginDate").InnerText ?? null,
                        HireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        PositionCodeDescription = EmploymentNode.SelectSingleNode("PostionTitle").InnerText ?? null,
                        JobOrderAssignment = EmploymentNode.SelectSingleNode("OrderOfAssignment").InnerText ?? null,
                        StaffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null,
                        EmpDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null,

                    };
                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }
        }   
        
        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private  string GetAssignmentAssociationId(string token, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffClassificationDescriptorId + Uri.EscapeDataString(Constants.staffClassificationDescriptorValue) + staffData.StaffClassification + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        dynamic original = JObject.Parse(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }).ToString());
                        var id = original.id;
                        if (id != null)
                            return id.ToString();
                    }
                    else
                    {
                        //Insert Data 
                        var rootObject = new StaffAssignmentDescriptor
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
                            StaffReference = new EdFiStaffReference
                            {
                                staffUniqueId = staffData.StaffUniqueIdValue,

                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },
                            EmploymentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                            {
                                educationOrganizationId = Constants.educationOrganizationIdValue,
                                staffUniqueId = staffData.StaffUniqueIdValue,
                                employmentStatusDescriptor = Constants.employmentStatusDescriptorValue+staffData.EmpDesc,
                                hireDate = staffData.HireDateValue,
                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },

                            StaffClassificationDescriptor = staffData.StaffClassification,
                            BeginDate = staffData.BeginDateValue,
                            EndDate = staffData.EndDateValue,
                            OrderOfAssignment = staffData.JobOrderAssignment,
                            PositionTitle = staffData.PositionCodeDescription
                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = _edfiApi.PostData(json, client, token);

                    }
                }
            }

            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue + ex.Message);

            }
            return null;
        }
        
        /// <summary>
        /// Updates the Position title in  [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private void UpdateAssignmentAssociation(string token, string id, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            try
            {

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + "/" + id);
                var rootObject = new StaffAssignmentDescriptor
                {
                    id = id,
                    EducationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = educationOrganizationId,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    StaffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffData.StaffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    EmploymentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        staffUniqueId = staffData.StaffUniqueIdValue,
                        employmentStatusDescriptor = Constants.employmentStatusDescriptorValue+staffData.EmpDesc,
                        hireDate = staffData.HireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffClassificationDescriptor = staffData.StaffClassification,
                    BeginDate = staffData.BeginDateValue,
                    EndDate = staffData.EndDateValue,
                    OrderOfAssignment = staffData.JobOrderAssignment,
                    PositionTitle = staffData.PositionCodeDescription
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = _edfiApi.PutData(json, client, token);
                _log.Info("Updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue);
            }
            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue + ex.Message);

            }
        }       
         
        /// <summary>
        /// Gets the data from the xml and updates StaffTelephone table for Staff Phone Numbers Cases.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffContact(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffContacts");
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>();
                foreach (XmlNode node in nodeList)
                {
                    var id = node.SelectSingleNode(@"ContactDetails/StaffUniqueId").InnerText;
                    List<StaffContactData> ContactNodeList = new List<StaffContactData>();
                    // Multiple contact numbers for same staffId
                    var dups = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/ContactDetails/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == id).Select(x => x.ParentNode.ParentNode).ToList();
                    foreach (var item in dups)
                    {
                        var staffContact = GetStaffContactXml(item);
                        ContactNodeList.Add(staffContact);
                    }
                    UpdatingStaffContactData(token, id, ContactNodeList);
                }

                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }
        /// <summary>
        /// Extract the Staff Contact data per node.
        /// </summary>
        /// <returns></returns>
        private StaffContactData GetStaffContactXml(XmlNode node)
        {
            try
            {
                StaffContactData staffAssignmentList = null;
                XmlNode StaffContactNode = node.SelectSingleNode("ContactDetails");
                if (StaffContactNode != null)
                {
                    staffAssignmentList = new StaffContactData
                    {
                        Id = StaffContactNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        TelephoneNumber = StaffContactNode.SelectSingleNode("Phone").InnerText ?? null,
                        TelephoneNumberTypeDescriptor = StaffContactNode.SelectSingleNode("Type").InnerText ?? null,
                        Ext = StaffContactNode.SelectSingleNode("Ext").InnerText ?? null,
                        OrderOfPriority = StaffContactNode.SelectSingleNode("Preferred").InnerText ?? null,
                        TextMessageCapabilityIndicator = true
                    };

                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }

        /// <summary>
        /// Extract the Staff Contact data per node.
        /// </summary>
        /// <returns></returns>
        private void UpdatingStaffContactData(string token, string staffUniqueId, List<StaffContactData> staffData)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueId);
                response = _edfiApi.GetData(client, token);

                StaffDescriptor data = JsonConvert.DeserializeObject<StaffDescriptor>(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }));
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        StaffReference rootObject = new StaffReference
                        {
                            StaffUniqueId = staffUniqueId,
                            FirstName = data.FirstName,
                            LastSurname = data.LastSurname,
                            telephones = staffData

                        };
                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = _edfiApi.PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl), token);
                        _log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueId);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueId + " Exception : " + ex.Message);

            }

        }

        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationAssignmentAssociation table for Transfer Cases.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffAssignmentDataTransferCases(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("TransferCases");
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation");
                var schoolDeptids = GetDeptList(configuration);
                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffAssignmentNodeList = GetAssignmentAssociationTransferXml(node);
                    if (staffAssignmentNodeList != null)
                    {
                        if (schoolDeptids.Count > 0)
                        {
                            string schoolid = null;
                            // Getting the EdOrgId for the Department ID 
                            var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue) && x.OperationalStatus.Equals("Active")).FirstOrDefault();
                            // setting the DeptId as EdOrgId for the staff, if no corresponding school is found
                            if (educationOrganizationId != null)
                            {
                                if (!string.IsNullOrEmpty(educationOrganizationId.SchoolId))
                                {
                                    //Inserting new Assignments and updating the postioTitle with JobCode - JobDesc
                                    if (!string.IsNullOrEmpty(staffAssignmentNodeList.StaffUniqueIdValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.BeginDateValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.StaffClassification))
                                    {
                                        StaffAssignmentAssociationData assignmentData = GetAssignmentAssociationIdTransfer(token, schoolid, staffAssignmentNodeList);
                                        if (assignmentData != null)
                                            UpdateAssignmentAssociationTransfer(token, assignmentData, staffAssignmentNodeList.EndDateValue);

                                    }
                                }
                            }                               
                                
                        }
                    }
                }
                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private StaffAssignmentAssociationData GetAssignmentAssociationTransferXml(XmlNode node)
        {
            try
            {
                StaffAssignmentAssociationData staffAssignmentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                if (staffNode == null && EducationNode == null) _log.Error("Nodes not reurning any data for Assignment");
                XmlNode staffClassificationNode = node.SelectSingleNode("StaffClassification");
                if (staffNode != null && EducationNode != null)
                {
                    staffAssignmentList = new StaffAssignmentAssociationData
                    {
                        StaffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        EducationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        EndDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        BeginDateValue = EmploymentNode.SelectSingleNode("BeginDate").InnerText ?? null,
                        StaffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null

                    };
                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }
        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private StaffAssignmentAssociationData GetAssignmentAssociationIdTransfer(string token, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffClassificationDescriptorId + Uri.EscapeDataString(Constants.staffClassificationDescriptorValue) + staffData.StaffClassification + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var original = JsonConvert.DeserializeObject<StaffAssignmentDescriptor>(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }));
                        var data = new StaffAssignmentAssociationData
                        {
                            Id = original.id,
                            StaffUniqueIdValue = original.StaffReference.staffUniqueId,
                            EducationOrganizationIdValue = original.EducationOrganizationReference.educationOrganizationId,
                            StaffClassification = original.StaffClassificationDescriptor,
                            BeginDateValue = original.BeginDate,
                            PositionCodeDescription = original.PositionTitle,
                            EndDateValue = original.EndDate,
                            JobOrderAssignment = original.OrderOfAssignment,
                            EmploymentEducationOrganizationIdValue = original.EmploymentStaffEducationOrganizationEmploymentAssociationReference.educationOrganizationId,
                            HireDateValue = original.EmploymentStaffEducationOrganizationEmploymentAssociationReference.hireDate,
                            EmpDesc = original.EmploymentStaffEducationOrganizationEmploymentAssociationReference.employmentStatusDescriptor

                        };
                        return data;

                    }
                }
            }

            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue + ex.Message);

            }
            return null;
        }      
        
       
        /// <summary>
        /// Updates the StaffEducationOrganizationAssignmentAssociation
        /// for Transfer cases.
        /// </summary>
        /// <returns></returns>
        private void UpdateAssignmentAssociationTransfer(string token, StaffAssignmentAssociationData assignmentData, string endDate)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + "/" + assignmentData.Id);
                var rootObject = new StaffAssignmentDescriptor
                {
                    EducationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = assignmentData.EducationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    StaffReference = new EdFiStaffReference
                    {
                        staffUniqueId = assignmentData.StaffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    EmploymentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        staffUniqueId = assignmentData.StaffUniqueIdValue,
                        employmentStatusDescriptor = Constants.employmentStatusDescriptorValue + assignmentData.EmpDesc,
                        hireDate = assignmentData.HireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    StaffClassificationDescriptor = assignmentData.StaffClassification,
                    BeginDate = assignmentData.BeginDateValue,
                    EndDate = endDate,
                    OrderOfAssignment = assignmentData.JobOrderAssignment,
                    PositionTitle = assignmentData.PositionCodeDescription
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = _edfiApi.PutData(json, client, token);
                _log.Info("Updating  StaffEducationOrganizationAssignmentAssociation for Staff Id Transfer and Retirement Case: " + assignmentData.StaffUniqueIdValue);

            }
            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id Transfer and Retirement Case: " + assignmentData.StaffUniqueIdValue + ex.Message);

            }
        }

        /// <summary>
        /// Gets Department/School list of the one existing in ODS.
        /// </summary>
        /// <returns></returns>
        private List<SchoolDept> GetDeptList(EdorgConfiguration configuration)
        {

            List<SchoolDept> existingDeptIds = new List<SchoolDept>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, _log);
                    existingDeptIds = restManager.GetSchoolList();
                }
                else
                {
                    _log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingDeptIds;
        }


        /// <summary>
        /// Gets Staff list of the one existing in ODS.
        /// </summary>
        /// <returns></returns>
        private List<string> GetStaffList(EdorgConfiguration configuration)
        {

            List<string> existingStaffIds = new List<string>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, _log);
                    existingStaffIds = restManager.GetStaffList();
                }
                else
                {
                    _log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingStaffIds;
        }

        /// <summary>
        /// Update Staff data in ODS.
        /// </summary>
        /// <returns></returns>
        private void UpdatingNewStaffData(string token, string staffUniqueIdValue, string fname, string mname, string lname, string birthDate)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                var rootObject = new StaffDescriptor
                {
                    StaffUniqueId = staffUniqueIdValue,
                    FirstName = fname,
                    MiddleName = mname,
                    LastSurname = lname,
                    BirthDate = birthDate

                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                if (!_restServiceManager.IsSuccessStatusCode((int)response.StatusCode) || (int)response.StatusCode == 404)
                {

                    response = _edfiApi.PostData(json, client, token);
                    _log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueIdValue);

                }
                else
                {
                    StaffAssociationReference data = JsonConvert.DeserializeObject<StaffAssociationReference>(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }));
                    var id = data.id;
                    response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + "/" + id), token);

                    _log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueIdValue);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueIdValue + " Exception : " + ex.Message);

            }
        }

        /// <summary>
        /// Update StaffSchoolAssociation with SchoolYear from endDate in ODS.
        /// </summary>
        /// <returns></returns>
        private void UpdateStaffSchoolAssociation(string token, string schoolId, string empDesc, string StaffUniqueIdValue, string url)
        {
            IRestResponse response = null;
            try
            {
                var endDate = GetAssignmentEndDate(token, StaffUniqueIdValue, empDesc, schoolId, url);
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl + Constants.programAssignmentDescriptor + Constants.schoolId + schoolId + Constants.staffUniqueId + StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                var rootObject = new StaffSchoolAssociation
                {
                    SchoolReference = new EdFiSchoolReference
                    {
                        schoolId = schoolId,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    SchoolYearTypeReference = new EdFiSchoolYearTypeReference
                    {
                        SchoolYear = GetSchoolYear(endDate).ToString(),

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffReference = new EdFiStaffReference
                    {
                        staffUniqueId = StaffUniqueIdValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    ProgramAssignmentDescriptor = "Regular Education",
                };
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {

                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    if (response.Content.Length <= 2)
                        response = _edfiApi.PostData(json, client, token);
                    else
                    {
                        var data = JsonConvert.DeserializeObject<List<StaffAssociationReference>>(response.Content);
                        foreach (var item in data)
                        {
                            var id = item.id;
                            response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl + "/" + id), token);
                        }
                    }

                }
                _log.Info("Updating  edfi.StaffAssociation for Schoolid : " + schoolId);
            }
            catch (Exception ex)
            {
                _log.Error("Error updating  edfi.StaffAssociation for Schoolid : " + schoolId + " Exception : " + ex.Message);
                _log.Error(ex.Message);
            }

        }

        /// <summary>
        /// Get school Year based on the endDate.
        /// </summary>
        /// <returns></returns>
        private int GetSchoolYear(string endDate)
        {
            DateTime date = DateTime.Today;
            int month = date.Month;
            int year = date.Year;
            try
            {
                if (!string.IsNullOrEmpty(endDate))
                {
                    month = DateTime.Parse(endDate).Month;
                    year = DateTime.Parse(endDate).Year;
                }
                else
                {
                    month = date.Month;
                    year = date.Year;
                }
                if (month > 6)
                    year = year + 1;
            }
            catch (Exception ex)
            {
                _log.Error("Error getting schoolYear :  Exception : " + ex.Message);
            }
            return year;
        }


    }
}
