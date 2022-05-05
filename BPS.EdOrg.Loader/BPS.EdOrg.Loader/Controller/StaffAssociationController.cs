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
using System.DirectoryServices;
using System.Web;
using System.DirectoryServices.AccountManagement;
using System.Text.RegularExpressions;

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
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().OrderBy(element => element.SelectSingleNode("EmploymentPeriod/EndDate").InnerText).ToList();
                
                var schoolDeptids = GetDeptList(configuration);
                foreach (XmlNode node in nodeList)
                {
                   
                    // Extracting the data froom the XMl file
                    var staffEmploymentNodeList = GetEmploymentAssociationXml(node);
                    var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffEmploymentNodeList.educationOrganizationIdValue) && x.OperationalStatus.Equals(Constants.OperationalStatusActive)).Select(n=>n.SchoolId).FirstOrDefault();
                    
                    if (staffEmploymentNodeList != null)
                    {
                        // Add new staff from peoplesoft file.
                        UpdateStaff(token,staffEmploymentNodeList);

                        //If there are more than one records,set enDate to null     
                        if (staffEmploymentNodeList.status == "T")
                        {
                            int countStd = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/StaffReference/StaffIdentity/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == staffEmploymentNodeList.staffUniqueIdValue).Distinct().Count();
                            if (countStd > 1)
                            {
                                //if (educationOrganizationId != null && educationOrganizationId.OperationalStatus.Equals(Constants.OperationalStatusInactive))
                                //    staffEmploymentNodeList.endDateValue = DateTime.Now.ToString("M/d/yyyy");
                                //else staffEmploymentNodeList.endDateValue = null;
                            }
                                

                        }
                        // Getting the Department from Assignment
                        var deptName = GetDepartmentName(staffEmploymentNodeList.staffUniqueIdValue, "1", staffEmploymentNodeList.empDesc, token);

                        // updating the values in Employment Association for 350000
                        if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.hireDateValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.empDesc))
                        {
                            staffEmploymentNodeList.department = deptName;
                            staffEmploymentNodeList.educationOrganizationIdValue = Constants.educationOrganizationIdValue;
                            string id = GetEmploymentAssociationId(token, staffEmploymentNodeList);
                            if (id != null)
                            {
                                string endDate = GetAssignmentEndDate(token, staffEmploymentNodeList.staffUniqueIdValue, staffEmploymentNodeList.empDesc, educationOrganizationId, Constants.StaffAssignmentUrl);

                                //Setting the Enddate with the one from AssignmentAssociation
                                if (endDate != null)
                               
                                     staffEmploymentNodeList.endDateValue = endDate.Split()[0];                               

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
 
        // Insert or update staff from PeopleSoft
        private void UpdateStaff(string token, StaffEmploymentAssociationData staffEmploymentNodeList)
        {
           
            if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.firstName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.lastName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.birthDate))
            {
                UpdatingNewStaffData(token, staffEmploymentNodeList);
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
                            unionCode = staffNode.SelectSingleNode("UnionCode").InnerText ?? null,
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
                                var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue) && x.OperationalStatus.Equals(Constants.OperationalStatusActive)).FirstOrDefault();

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
        /// Gets the Work email from LDAP and Home Email from PPsft file and updates the ODS.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffEmailData(string token, EdorgConfiguration configuration)
        {
            

            try
            {
                var staffEmailsLDAP = GetStaffEmail(token);
                var staffEmailsHome = GetStaffEmailPersonal(configuration, token, _log);

                
                bool indicator = true;
               
                foreach (var item in staffEmailsLDAP)
                {
                    List<StaffElectronicMailsData> respEmail = new List<StaffElectronicMailsData>();
                    
                        var staffEmailPersonal = staffEmailsHome.Where(x => x.Id.Equals(item.Key)).FirstOrDefault();
                        if (staffEmailPersonal != null)
                        {
                            if (staffEmailPersonal.electronicMailAddress != null) respEmail.Add(new StaffElectronicMailsData { Id = item.Key, electronicMailAddress = staffEmailPersonal.electronicMailAddress, electronicMailTypeDescriptor = "uri://ed-fi.org/ElectronicMailTypeDescriptor#Home/Personal", primaryEmailAddressIndicator = staffEmailPersonal.primaryEmailAddressIndicator });
                            indicator = staffEmailPersonal.primaryEmailAddressIndicator;
                        }
                        var staffEmail = staffEmailsLDAP.Where(x => x.Key.Equals(item.Key)).Select(a => a.Value).FirstOrDefault();

                        if (staffEmail != null) respEmail.Add(new StaffElectronicMailsData { Id = item.Key, electronicMailAddress = staffEmail, electronicMailTypeDescriptor = "uri://ed-fi.org/ElectronicMailTypeDescriptor#Work", primaryEmailAddressIndicator = Constants.GetPrimaryIndicator(indicator.ToString()) });
                        UpdateStaffLDAPEmail(respEmail, token);
                

            }

            }




            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }


        /// <summary>
        /// Gets the data from the xml and updates Department table.
        /// </summary>
        /// <returns></returns>
        public void UpdateEducationServiceCenter(string token, EdorgConfiguration configuration)
        {
            try
            {
                var schoolDeptids = GetDeptList(configuration);
                
                XmlDocument xmlDoc = _prseXML.LoadXml("EducationOrganization");
                var nodeList = xmlDoc.SelectNodes(@"//StaffEducationOrganizationAssociation/EducationServiceCenter");
                
                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var serviceCenterNodeList = GetEducationServiceCenterXml(node);
                    var rootObject = new EducationServiceCenterData
                    {
                        Categories = serviceCenterNodeList.Categories,
                        EducationServiceCenterId = serviceCenterNodeList.EducationServiceCenterId,
                        Addresses = serviceCenterNodeList.Addresses,
                        IdentificationCodes = serviceCenterNodeList.IdentificationCodes,
                        NameOfInstitution = serviceCenterNodeList.NameOfInstitution,
                        ShortNameOfInstitution = serviceCenterNodeList.ShortNameOfInstitution,
                        OperationalStatusDescriptor = "uri://ed-fi.org/OperationalStatusDescriptor#"+Constants.Active

                    };

                    // Posting the Data if it is not already inserted by Aspen as School
                    if (serviceCenterNodeList != null)
                    {
                        // Verifying if the Dept already exist in EducationOrgIdentification code table 
                        var result = schoolDeptids.Find(x => x.DeptId == serviceCenterNodeList.EducationServiceCenterId);
                        if (result == null)
                        {
                            IRestResponse response = null;
                            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.EducationServiceCenter);
                            response = _edfiApi.GetData(client, token);
                            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                            {
                                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                                response = _edfiApi.PostData(json, client, token);
                                _log.Info("Updated EducationServiceCenter for Staff Id : ");
                                

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
        // <summary>
        /// Gets the data from the xml and updates StaffAddress from PPsft files.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffAddress(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffAddressEmployee");
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");

                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAddressAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().ToList();
           
                foreach (XmlNode node in nodeList)
                {
                    var id = node.SelectSingleNode(@"StaffAddress/StaffUniqueId").InnerText;
                    // Extracting the data froom the XMl file
                    var staffAddressNodeList = GetStaffAddressXml(node);
                    UpdatingStaffAddressData(token,id, staffAddressNodeList);

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
        /// Gets the data from the xml and updates StaffTelephone table for Staff Phone Numbers Cases.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffContact(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffContacts");
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>();
                
                foreach (XmlNode node in nodeList)
                {
                    var id = node.SelectSingleNode(@"ContactDetails/StaffUniqueId").InnerText;
                    List<StaffContactData> ContactNodeList = new List<StaffContactData>();                    
                    // Multiple contact numbers for same staffId
                    var dups = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/ContactDetails/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == id).Select(x=>x.ParentNode.ParentNode).ToList();
               
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
        /// Gets the data from the xml and updates StaffEducationOrganizationAssignmentAssociation table for Transfer Cases.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffAssignmentDataTransferCases(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("TransferCases");
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");

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
                            var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue) && x.OperationalStatus.Equals(Constants.OperationalStatusActive)).FirstOrDefault();

                            // setting the DeptId as EdOrgId for the staff, if no corresponding school is found
                            if (educationOrganizationId != null) schoolid = educationOrganizationId.SchoolId;
                            if (!string.IsNullOrEmpty(schoolid))
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


                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

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
        private EducationServiceCenterData GetEducationServiceCenterXml(XmlNode node)
        {
            try
            {
                EducationServiceCenterData serviceCenterList = null;
                XmlNode EducationOrgNode = node.SelectSingleNode("EducationOrganizationIdentificationCode");
                //XmlNode InstitutionNameNode = node.SelectSingleNode("NameOfInstitution");
                //XmlNode ShortInstitutionNameNode = node.SelectSingleNode("NameOfInstitution");                
               // XmlNode EducationOrganizationCategoryDescriptorNode = node.SelectSingleNode("EducationOrganizationCategoryDescriptor");
                XmlNode AddressNode = node.SelectSingleNode("Address");
                //XmlNode EducationServiceCenterNode = node.SelectSingleNode("EducationServiceCenterId");

               // if (EducationOrganizationCategoryDescriptorNode == null && EducationServiceCenterNode == null) _log.Error("Nodes not reurning any data for Assignment");

               
                if (AddressNode != null && EducationOrgNode != null)
                {
                    serviceCenterList = new EducationServiceCenterData
                    {
                        Categories = new List<ServiceCategoryDescriptor>
                        {
                            new ServiceCategoryDescriptor{
                            EducationOrganizationCategoryDescriptor = node.SelectSingleNode("EducationOrganizationCategoryDescriptor").InnerText ?? null,
                            }
                        },
                       
                        EducationServiceCenterId = node.SelectSingleNode("EducationServiceCenterId").InnerText ?? null,

                        Addresses = new List<ServiceAddresses>
                        {
                            new ServiceAddresses
                            {
                            AddressTypeDescriptor = AddressNode.SelectSingleNode("AddressTypeDescriptor").InnerText ?? null,
                            StateAbbreviationDescriptor = AddressNode.SelectSingleNode("StateAbbreviationDescriptor").InnerText ?? null,
                            City = AddressNode.SelectSingleNode("City").InnerText ?? null,
                            StreetNumberName = AddressNode.SelectSingleNode("StreetNumberName").InnerText ?? null,
                            PostalCode = AddressNode.SelectSingleNode("PostalCode").InnerText ?? null,
                            }
                           
                        },
                       
                        IdentificationCodes = new List<ServiceIdentificationCode>
                        {
                            new ServiceIdentificationCode
                            {
                            EducationOrganizationIdentificationSystemDescriptor = EducationOrgNode.SelectSingleNode("educationOrganizationIdentificationSystemDescriptor").InnerText ?? null,
                            IdentificationCode = EducationOrgNode.SelectSingleNode("IdentificationCode").InnerText ?? null,

                            }

                        },
                         NameOfInstitution = node.SelectSingleNode("NameOfInstitution").InnerText ?? null,
                         ShortNameOfInstitution = node.SelectSingleNode("NameOfInstitution").InnerText ?? null,
                    };

                }
                return serviceCenterList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }
        private StaffContactData GetStaffContactXml(XmlNode node)
        {
            try
            {
                StaffContactData staffAssignmentList = null;
               // XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode StaffContactNode = node.SelectSingleNode("ContactDetails");              
                               
                if (StaffContactNode != null)
                {
                    staffAssignmentList = new StaffContactData
                    {
                        Id = StaffContactNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        telephoneNumber = StaffContactNode.SelectSingleNode("Phone").InnerText ?? null,
                        telephoneNumberTypeDescriptor = StaffContactNode.SelectSingleNode("Type").InnerText ?? null,
                        ext = StaffContactNode.SelectSingleNode("Ext").InnerText ?? null,
                        orderOfPriority = StaffContactNode.SelectSingleNode("Preferred").InnerText ?? null,
                        textMessageCapabilityIndicator = true
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


        private List<StaffAddressData> GetStaffAddressXml(XmlNode node)
        {
            try
            {
                List<StaffAddressData> staffAssignmentList = new List<StaffAddressData>();
                // XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode StaffAddressNode = node.SelectSingleNode("StaffAddress");

                if (StaffAddressNode != null)
                {
                    var staffAddress = new StaffAddressData
                    {
                        Id = StaffAddressNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        addressTypeDescriptor = StaffAddressNode.SelectSingleNode("StaffAddressType").InnerText ?? null,
                        streetNumberName = StaffAddressNode.SelectSingleNode("StaffAddress").InnerText ?? null,
                        city = StaffAddressNode.SelectSingleNode("StaffCity").InnerText ?? null,
                        stateAbbreviationDescriptor = StaffAddressNode.SelectSingleNode("StaffState").InnerText ?? null,
                        postalCode = StaffAddressNode.SelectSingleNode("StaffZip").InnerText ?? null,
                        localeDescriptor = StaffAddressNode.SelectSingleNode("StaffLocale").InnerText ?? null

                    };
                    staffAssignmentList.Add(staffAddress);
                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }
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

        private string GetAssignmentEndDate(string token, string staffUniqueId,string empDesc, string schoolId, string url)
        {
            string endDate = null;
            DateTime maxValue = default(DateTime); 
            try
            {
                IRestResponse response = null;

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + url +  Constants.educationOrganizationId + schoolId + Constants.staffUniqueId + staffUniqueId + Constants.GetEmpStatusDescp(empDesc)+ empDesc);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {                        
                        var original = JsonConvert.DeserializeObject<List<UpdateEndDateStaff>>(response.Content);

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
                            if (result >=0)
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

        private void UpdatingNewStaffData(string token, StaffEmploymentAssociationData staffNodeList)
        {
            try
            {
                IRestResponse response = null;
                List<StaffDescriptor> resp = null;               
                List<StaffContactData> respTel = null;
                List<StaffElectronicMailsData> respEmail = null;

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffNodeList.staffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.ContentLength > 2)
                    {
                        resp = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                        foreach( var item in resp)
                        {             
                            //foreach (var email in item.ElectronicMails)
                            //{
                            //    var obj = item.ElectronicMails.Find(x => (x.electronicMailAddress == email.electronicMailAddress));
                            //    if (!item.ElectronicMails.Contains(obj))
                            //        item.ElectronicMails.Add(email);
                            //}
                            respTel = item.Telephones;
                            respEmail = item.ElectronicMails;
                        }

                    }
                   

                    var rootObject = new StaffDescriptor
                    {
                        StaffUniqueId = staffNodeList.staffUniqueIdValue,
                        FirstName = staffNodeList.staff.firstName,
                        MiddleName = staffNodeList.staff.middleName,
                        LastSurname = staffNodeList.staff.lastName,
                        BirthDate = staffNodeList.staff.birthDate,                        
                        //ElectronicMails = staffEmail,

                        ElectronicMails = respEmail,
                        //new List<StaffElectronicMailsData>
                        //{
                        //    new StaffElectronicMailsData
                        //    {
                        //        electronicMailAddress = staffEmail,
                        //        primaryEmailAddressIndicator = true,
                        //        electronicMailTypeDescriptor = "uri://ed-fi.org/ElectronicMailTypeDescriptor#Work"
                        //    }
                        //},


                        Telephones = respTel,
                        IdentificationCodes = new List<EdFiIdentificationCode> {
                        new EdFiIdentificationCode
                            {
                                AssigningOrganizationIdentificationCode = null,
                                StaffIdentificationSystemDescriptor = Constants.StaffIdentificationSystemDescriptor,
                                IdentificationCode = staffNodeList.staffUniqueIdValue


                            }
                        },
                        _ext = new StaffEdFiExtension()
                        {
                            Staff = new StaffExtension()
                            {
                                unionCode = staffNodeList.staff.unionCode
                            }
                        }


                    };
                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    if (response.Content.Length > 2)
                    {
                        List<StaffDescriptor> data = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                           var id = data[0].Id;
                            response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + "/" + id), token);
                            _log.Info("Updating  edfi.staff for Staff Id : " + staffNodeList.staffUniqueIdValue);

                       

                    }
                    else
                    {                     
                        response = _edfiApi.PostData(json, client, token);
                        _log.Info("Inserting  edfi.staff for Staff Id : " + staffNodeList.staffUniqueIdValue);                        
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffNodeList.staffUniqueIdValue + " Exception : " + ex.Message);

            }



        }

        private void UpdatingStaffContactData(string token,string staffUniqueId, List<StaffContactData> staffData)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueId);
                response = _edfiApi.GetData(client, token);
                var original = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                foreach (var data in original)
                    {
                    if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                    {
                        if (staffData != null)
                        {
                            StaffDescriptor rootObject = new StaffDescriptor
                            {
                                StaffUniqueId = staffUniqueId,
                                FirstName = data.FirstName,
                                MiddleName = data.MiddleName,
                                LastSurname = data.LastSurname,
                                BirthDate = data.BirthDate,
                                IdentificationCodes = data.IdentificationCodes,
                                Telephones = staffData,
                                Addresses = data.Addresses,
                                //_ext = data._ext,
                                ElectronicMails = data.ElectronicMails
                            };
                            string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                            response = _edfiApi.PostData(json, client, token);
                            _log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueId);
                        }


                    }

                }
                

          
            }
            catch (Exception ex)
            {
               _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueId + " Exception : " + ex.Message);

            }

        }


        private void UpdatingStaffAddressData(string token,string staffUniqueId, List<StaffAddressData> staffData)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueId);
                response = _edfiApi.GetData(client, token);
                var original = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                foreach (var data in original)
                {
                    if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                    {
                        if (staffData != null)
                        {
                            StaffDescriptor rootObject = new StaffDescriptor
                            {
                                StaffUniqueId = data.StaffUniqueId,
                                FirstName = data.FirstName,
                                MiddleName = data.MiddleName,
                                LastSurname = data.LastSurname,
                                BirthDate = data.BirthDate,
                                IdentificationCodes = data.IdentificationCodes,
                                Telephones = data.Telephones,
                                Addresses = staffData,
                               // _ext = data._ext,
                                ElectronicMails = data.ElectronicMails


                            };
                            string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                            response = _edfiApi.PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl), token);
                            _log.Info("Updating  edfi.staffAddress for Staff Id : " + staffUniqueId);
                        }


                    }

                }



            }
            catch (Exception ex)
            {
                _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueId + " Exception : " + ex.Message);

            }

        }


        private IDictionary<string,string> GetStaffEmail(string token)
        {
            try
            {
                IDictionary<string, string> staffEmail = new Dictionary<string, string>();
                DirectoryEntry directoryEntry = new DirectoryEntry("LDAP://cdnimda04.admin.mybps.org/OU=staff,DC=admin,DC=mybps,DC=org");
                DirectorySearcher dSearcher = new DirectorySearcher(directoryEntry);
                string filter = "(objectClass=user)";
                dSearcher.Filter = filter;
                dSearcher.PageSize = 15000;
                var results = dSearcher.FindAll();
                int count = results.Count;
                foreach (SearchResult sr in results)
                {
                    PrincipalContext context = new PrincipalContext(ContextType.Domain);                   
                    var userName = GetProperty(sr, "samAccountName");
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userName);
                    var email = GetProperty(sr, "mail");
                    if (user.Enabled == true) _log.Info("The user is enabled  through AD : " + userName + " Email : " + email);
                    else _log.Info("The user is disabled through AD : " + userName + " Email : " + email);                     
                    staffEmail.Add(userName, email);
                    if (userName.StartsWith("4000") || userName.StartsWith("X0"))
                    {                    
                            //adding Sponsored Staff to email
                            var firstName = GetProperty(sr, "givenname");
                            var lastName = GetProperty(sr, "sn");
                            AddSponsoredStaff(userName, firstName, lastName, token);
                    }
                    
                    
                }
                return staffEmail;
            }
            catch(Exception ex)
            {
                _log.Error(" Error getting staffemails from LDAP for Staff Id : " +  ex.Message);
                return null;
            }

        }


        private List<StaffElectronicMailsData> GetStaffEmailPersonal(EdorgConfiguration configuration, string token, ILog logger)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadXml("StaffEmailPersonal");
                _restServiceManager = new RestServiceManager(configuration, token, _log);
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffEmailAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().ToList();
                List<StaffElectronicMailsData> staffEmailList = new List<StaffElectronicMailsData>();
                StaffElectronicMailsData staffEmail = null;
                foreach (XmlNode node in nodeList)
                {
                    XmlNode staffNode = node.SelectSingleNode("StaffPersonalEmail");
                    if (staffNode != null)
                    {
                        staffEmail = new StaffElectronicMailsData
                        {                           
                            Id = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                            electronicMailAddress = staffNode.SelectSingleNode("StaffEmail").InnerText ?? null,
                            electronicMailTypeDescriptor = staffNode.SelectSingleNode("Type").InnerText ?? null,
                            primaryEmailAddressIndicator = Constants.GetBoolIndicator(staffNode.SelectSingleNode("EmailAddressIndicator").InnerText)

                        };
                    }
                    if (!staffEmail.electronicMailAddress.EndsWith("@boston.k12.ma.us")&& !Regex.IsMatch(staffEmail.electronicMailAddress, @"^\d+"))
                      staffEmailList.Add(staffEmail);
                }
                

                return staffEmailList;
            }
            
            catch (Exception ex)
            {
                _log.Error(" Error getting staffemails from PPsft for Staff Id : " + ex.Message);
                return null;
            }

        }

        private static string GetProperty(SearchResult searchResult, string PropertyName)
        {
            if (searchResult.Properties.Contains(PropertyName))
            {
                return searchResult.Properties[PropertyName][0].ToString();
            }
            else
            {
                return string.Empty;
            }
        }


        private void AddSponsoredStaff(string uName, string fname, string lName, string token)
        {
            List<StaffElectronicMailsData> respEmail = null;
            List<StaffContactData> respTel = null;
            List<StaffDescriptor> resp = null;
            List<StaffAddressData> respAddr = null;
            IRestResponse response = null;


            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + uName);
            response = _edfiApi.GetData(client, token);
            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.ContentLength > 2)
                {
                    resp = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                    foreach (var item in resp)
                    {
                        respAddr = item.Addresses;
                        respTel = item.Telephones;
                        respEmail = item.ElectronicMails;
                    }

                }
            }
            var rootObject = new StaffDescriptor
            {
                StaffUniqueId = uName,
                FirstName = fname,               
                LastSurname = lName,
                ElectronicMails = respEmail,
                Telephones = respTel,
                Addresses = respAddr

            };
            string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
            if (response.Content.Length > 2)
            {
                List<StaffDescriptor> data = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                var id = data[0].Id;
                response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + "/" + id), token);
                _log.Info("Updating  edfi.staff for Staff Id : " + uName);



            }
            else
            {
                response = _edfiApi.PostData(json, client, token);
                _log.Info("Inserting  edfi.staff for Staff Id : " + uName);
            }
        }
        private void UpdateStaffLDAPEmail(List<StaffElectronicMailsData> respstaffEmail, string token)
        {
            List<StaffContactData> respTel = null;
            List<StaffDescriptor> resp = null;
            List<StaffAddressData> respAddr = null;
            IRestResponse response = null;
            string fName = null;
            string mName = null;
            string lName = null;
            string birthdate = null;

            foreach (var staff in respstaffEmail)
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staff.Id);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.ContentLength > 2)
                    {
                        resp = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                        foreach (var item in resp)
                        {
                            fName = item.FirstName;
                            mName = item.MiddleName;
                            lName = item.LastSurname;
                            birthdate = item.BirthDate;
                            respTel = item.Telephones;
                            respAddr = item.Addresses;
                        }

                    }
                }
                var rootObject = new StaffDescriptor
                {
                    
                    FirstName = fName,
                    LastSurname = lName,
                    MiddleName = mName,
                    BirthDate = birthdate,
                    StaffUniqueId = staff.Id,
                    ElectronicMails = respstaffEmail,
                    Telephones = respTel,
                    Addresses = respAddr

                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                if (response.Content.Length > 2)
                {
                    List<StaffDescriptor> data = JsonConvert.DeserializeObject<List<StaffDescriptor>>(response.Content);
                    var id = data[0].Id;
                    response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + "/" + id), token);
                    _log.Info("Updating  edfi.staff for Staff Id : " + staff.Id);



                }
                else
                {
                    response = _edfiApi.PostData(json, client, token);
                    _log.Info("Inserting  edfi.staff for Staff Id : " + staff.Id);
                }
            }
        
        }

        /// <summary>
        /// Get DepartmentName from [edfi.EducationServiceCenter] table.
        /// </summary>
        /// <returns></returns>
        private string GetDepartmentName(string staffUniqueId, string orderofAssignment,string empDesc, string token)
        {
            string dept = null;
            string EdOrgId = null;
            DateTime maxValue = DateTime.MinValue;
            IRestResponse response = null;

            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.staffUniqueId1 + staffUniqueId + Constants.orderofAssignment + orderofAssignment + Constants.GetEmpStatusDescp(empDesc)+ empDesc);
            response = _edfiApi.GetData(client, token);
            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length > 2)
                {
                    var original = JsonConvert.DeserializeObject<List<StaffAssignmentDescriptor>>(response.Content);
                    foreach (var data in original)
                    {
                        if (string.IsNullOrEmpty(data.EndDate))
                        {
                            DateTime beginDate;
                            DateTime.TryParse(data.BeginDate, out beginDate);
                            int result = DateTime.Compare(beginDate, maxValue);
                            if (result > 0)
                            {
                                maxValue = beginDate;
                            }

                    }


                }
                    EdOrgId = original.Where(a => a.BeginDate == maxValue.ToString("yyyy-MM-dd")).Select(x => x.EducationOrganizationReference.educationOrganizationId).FirstOrDefault();

                    if (!String.IsNullOrEmpty(EdOrgId) && EdOrgId != null)
                        dept = GetNameOfInstitution(EdOrgId, token);
                }
            }

            return dept;
        }

        /// <summary>
        /// Get DepartmentName from [edfi.EducationServiceCenter] table.
        /// </summary>
        /// <returns></returns>
        private string GetNameOfInstitution(string schoolId, string token)
        {
            IRestResponse response = null;
            string nameOfInstitution = null;
            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.SchoolUrl + Constants.schoolId1 + schoolId);
            response = _edfiApi.GetData(client, token);
            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length > 2)
                {
                    dynamic original = JsonConvert.DeserializeObject(response.Content);
                    foreach (var data in original)
                    {
                        nameOfInstitution = schoolId +" - "+ data.shortNameOfInstitution;
                    }

                }
            }
            return nameOfInstitution;
        }
            
        
        
        
        
        
        //}
            /// <summary>
            /// Get the Id from the [StaffEducationOrganizationEmploymentAssociation] table.
            /// </summary>
            /// <returns></returns>
            private string GetEmploymentAssociationId(string token, StaffEmploymentAssociationData staffData)
        {
            IRestResponse response = null;

            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + Constants.educationOrganizationId + staffData.educationOrganizationIdValue + Constants.GetEmpStatusDescp(staffData.empDesc) + staffData.empDesc + Constants.hireDate + staffData.hireDateValue + Constants.staffUniqueId + staffData.staffUniqueIdValue);
            string id = null;
            response = _edfiApi.GetData(client, token);
            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
            {

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
                    employmentStatusDescriptor = Constants.GetEmpStatusDescpField(staffData.empDesc) + staffData.empDesc,
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue,
                    department = staffData.department
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                if (response.Content.Length > 2)
                {
                    //dynamic original = JObject.Parse(response.Content.ToString());
                    dynamic original = JsonConvert.DeserializeObject(response.Content);
                    foreach(var data in original)
                    {
                        id = data.id;
                        if (id != null)
                        {
                            var resp = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + "/" + id), token);
                            _log.Info("Updated into StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
                            return id;
                        }
                            
                    }
                    
                }
                else
                {
                    //Insert Data                      
                    var resp = _edfiApi.PostData(json, client, token);
                    _log.Info("Inserted into StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
                }
            }

            return id;
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
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffUniqueId + staffData.StaffUniqueIdValue +  Constants.StaffClassificationDescriptor1 + staffData.StaffClassification );
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        
                        var data = JsonConvert.DeserializeObject<List<StaffAssignmentDescriptor>>(response.Content);
                        foreach(var item in data)
                        {
                            var id = item.id;
                            if (id != null)
                                return id.ToString();

                        }
                       

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
                                employmentStatusDescriptor = Constants.GetEmpStatusDescpField(staffData.EmpDesc) + staffData.EmpDesc,
                                hireDate = staffData.HireDateValue,
                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },

                            StaffClassificationDescriptor = Constants.StaffClassificationDescriptorField + staffData.StaffClassification,
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
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private StaffAssignmentAssociationData GetAssignmentAssociationIdTransfer(string token, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffClassificationDescriptorId + staffData.StaffClassification + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        //dynamic original = JObject.Parse(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }).ToString());
                        var original = JsonConvert.DeserializeObject<StaffAssignmentDescriptor>(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }));
                        var data = new StaffAssignmentAssociationData
                        {
                            Id = original.id,
                            StaffUniqueIdValue = original.StaffReference.staffUniqueId,
                            EducationOrganizationIdValue = original.EducationOrganizationReference.educationOrganizationId,
                            StaffClassification = Constants.StaffClassificationDescriptorField+original.StaffClassificationDescriptor,
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
                        employmentStatusDescriptor = Constants.GetEmpStatusDescpField(staffData.EmpDesc) + staffData.EmpDesc,
                        hireDate = staffData.HireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffClassificationDescriptor = Constants.StaffClassificationDescriptorField+staffData.StaffClassification,
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
        /// Updates the Position title in  [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private void UpdateAssignmentAssociationTransfer(string token, StaffAssignmentAssociationData assignmentData,string endDate)
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
                        employmentStatusDescriptor = Constants.GetEmpStatusDescpField(assignmentData.EmpDesc) +assignmentData.EmpDesc,
                        hireDate = assignmentData.HireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffClassificationDescriptor = Constants.StaffClassificationDescriptorField+assignmentData.StaffClassification,
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
        /// Updates the enddate to [StaffEducationOrganizationAssignmentAssociation] table.
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
                    employmentStatusDescriptor = Constants.GetEmpStatusDescpField(staffData.empDesc) +staffData.empDesc,
                    
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue,
                    department = staffData.department
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


        private void UpdateStaffSchoolAssociation(string token, string schoolId,string empDesc, string StaffUniqueIdValue, string url)
        {
            
            IRestResponse response = null;
            try
            {
                var endDate = GetAssignmentEndDate(token, StaffUniqueIdValue, empDesc, schoolId,url);
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl +Constants.schoolId1 + Constants.GetSchooId(schoolId) + Constants.staffUniqueId + StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                var rootObject = new StaffSchoolAssociation
                {
                    SchoolReference = new EdFiSchoolReference
                    {
                        schoolId = Constants.GetSchooId(schoolId),
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
                    ProgramAssignmentDescriptor = Constants.ProgramAssignmentDescriptorField,
                };

                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                   
                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    if (response.Content.Length <= 2)
                    {
                        response = _edfiApi.PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl), token);
                        _log.Info("Inserting edfi.StaffAssociation for Schoolid : " + schoolId);
                    }
                        
                    else
                    {
                        var data = JsonConvert.DeserializeObject<List<StaffAssociationReference>>(response.Content);
                        foreach(var item in data)
                        {
                            var id = item.id;
                            response = _edfiApi.PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl + "/" + id), token);
                            _log.Info("Updating  edfi.StaffAssociation for Schoolid : " + schoolId);
                        }
                        
                       
                        
                    }
                       
                }
               
                }
            
            catch (Exception ex)
            {
                _log.Error("Error updating  edfi.StaffAssociation for Schoolid : " + schoolId + " Exception : " + ex.Message);
                _log.Error(ex.Message);
            }

        }

        private  int GetSchoolYear(string endDate)
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
        private List<SchoolDept> GetDeptList(EdorgConfiguration configuration)
        {

            List<SchoolDept> existingEdOrgIds = new List<SchoolDept>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, _log);                   
                    existingEdOrgIds.AddRange(restManager.GetSchoolList());
                    existingEdOrgIds.AddRange(restManager.GetDeptsList());
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

            return existingEdOrgIds;
        }

        private  List<string> GetStaffList(EdorgConfiguration configuration)
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
    }
}
