using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Linq;
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.MetaData;
using System.Xml.Linq;

namespace BPS.EdOrg.Loader.XMLDataLoad
{
    class ParseXmls
    {
        private readonly EdorgConfiguration _configuration = null;      
        private readonly ILog _log;
        public ParseXmls(EdorgConfiguration configuration, ILog logger)
        {
            _configuration = configuration;            
            _log = logger;
        }

        public void CreateXml()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"EducationOrganization-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeEducationOrganization");           

                string dataFilePath = _configuration.DataFilePath;
                string[] lines = File.ReadAllLines(dataFilePath);

                int i = 0;
                int deptIdIndex = 0;
                int deptTitleIndex = 0;
                int locationIndex = 0;
                int numberOfRecordsCreatedInXml = 0, numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        deptIdIndex = Array.IndexOf(header, "DeptID");
                        deptTitleIndex = Array.IndexOf(header, "Dept Title");
                        locationIndex = Array.IndexOf(header, "Location");
                        if (deptIdIndex < 0 || deptTitleIndex < 0 || locationIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains the DeptID or Dept Title headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        string deptId = fields[deptIdIndex]?.Trim();
                        string deptTitle = fields[deptTitleIndex]?.Trim();
                        string location = fields[locationIndex]?.Trim();

                        _log.Debug($"Creating node for {deptId}-{deptTitle}-{location}");
                        CreateNode(deptId, deptTitle, location, writer);
                        
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    _log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    _log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                _log.Info("CreateXML ended successfully");


            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating Dept XML , Exception: {ex.Message}");
            }
        }
        public  void CreateXmlJob()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffAssociation-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAssociation");
               
                string dataFilePath = _configuration.DataFilePathJob;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0; int actionDtIndex = 0; int actionIndex = 0;
                int deptIdIndex = 0; int unionCodeIndex = 0; int emplClassIndex = 0; int jobIndicatorIndex = 0; int statusIndex = 0;
                int middleNameIndex = 0; int actionDateIndex = 0; int hireDateIndex = 0; int jobCodeIndex = 0; int jobTitleIndex = 0;
                int entryDateIndex = 0; int firstNameIndex = 0; int lastNameIndex = 0; int birthDateIndex = 0;
                int numberOfRecordsCreatedInXml = 0, numberOfRecordsSkipped = 0; int empIdIndex = 0; int locationIndex = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        empIdIndex = Array.IndexOf(header, "ID");
                        deptIdIndex = Array.IndexOf(header, "Deptid");
                        actionIndex = Array.IndexOf(header, "Action");
                        actionDtIndex = Array.IndexOf(header, "Action Dt");
                        actionDateIndex = Array.IndexOf(header, "Eff Date");
                        hireDateIndex = Array.IndexOf(header, "Orig Hire Date");
                        jobCodeIndex = Array.IndexOf(header, "Job Code");
                        jobTitleIndex = Array.IndexOf(header, "Job Title");
                        entryDateIndex = Array.IndexOf(header, "Job Entry Date");
                        unionCodeIndex = Array.IndexOf(header, "Union Code");
                        emplClassIndex = Array.IndexOf(header, "Empl Class");
                        jobIndicatorIndex = Array.IndexOf(header, "Job Indicator");
                        statusIndex = Array.IndexOf(header, "Status");
                        firstNameIndex = Array.IndexOf(header, "First Name");
                        middleNameIndex = Array.IndexOf(header, "Middle Name");
                        lastNameIndex = Array.IndexOf(header, "Last Name");
                        birthDateIndex = Array.IndexOf(header, "Birthdate");
                        locationIndex = Array.IndexOf(header, "Location");


                        if (deptIdIndex < 0 || actionIndex < 0 || empIdIndex < 0 || hireDateIndex < 0 || entryDateIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains the ID or JobCode or ActionDt headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var staffAssociationData = new StaffAssociationData
                        {
                            staffId = fields[empIdIndex]?.Trim(),
                            deptId = fields[deptIdIndex]?.Trim(),
                            action = fields[actionIndex]?.Trim(),
                            actionDate = fields[actionDtIndex]?.Trim(),
                            endDate = fields[actionDateIndex]?.Trim(),
                            hireDate = fields[hireDateIndex]?.Trim(),
                            jobCode = fields[jobCodeIndex]?.Trim(),
                            jobTitle = fields[jobTitleIndex]?.Trim(),
                            entryDate = fields[entryDateIndex]?.Trim(),
                            unionCode = fields[unionCodeIndex]?.Trim(),
                            empClass = fields[emplClassIndex]?.Trim(),
                            jobIndicator = fields[jobIndicatorIndex]?.Trim(),
                            status = fields[statusIndex]?.Trim(),
                            firstName = fields[firstNameIndex]?.Trim(),
                            middleName = fields[middleNameIndex]?.Trim(),
                            lastName = fields[lastNameIndex]?.Trim(),
                            birthDate = fields[birthDateIndex]?.Trim(),
                            location = fields[locationIndex]?.Trim()

                        };

                        string descCode = Constants.StaffClassificationDescriptorCode(staffAssociationData.jobCode, int.Parse(staffAssociationData.deptId), staffAssociationData.unionCode).ToString().Trim();
                        string empClassCode = Constants.EmpClassCode(staffAssociationData.empClass);
                        string jobOrderAssignment = Constants.JobOrderAssignment(staffAssociationData.jobIndicator);

                        _log.Debug($"Creating node for {staffAssociationData.staffId}-{staffAssociationData.deptId}-{staffAssociationData.endDate}");
                        CreateNodeJob(staffAssociationData, descCode, empClassCode, jobOrderAssignment, writer);
                        numberOfRecordsCreatedInXml++;

                        XmlNodeList nodeList = GetDeptforLocation();
                        if (nodeList.Count > 0)
                        {
                            var deptIdLocation = nodeList.Cast<XmlNode>().Where(n => n["Location"].InnerText == staffAssociationData.location).Select(x => x["StateOrganizationId"].InnerText).FirstOrDefault();
                            if (deptIdLocation != null)
                            {
                                // If departments are different that means Physical location is different so  we have 2 assignments for the staff
                                if (!staffAssociationData.deptId.Equals(deptIdLocation))
                                {
                                    staffAssociationData.deptId = deptIdLocation;
                                    _log.Debug($"Creating node for {staffAssociationData.staffId}-{staffAssociationData.deptId}-{staffAssociationData.endDate}");
                                    CreateNodeJob(staffAssociationData, descCode, empClassCode, jobOrderAssignment, writer);
                                    numberOfRecordsCreatedInXml++;
                                }
                            }
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    _log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    _log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                _log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating JobCode XML , Exception: {ex.Message}");
            }
        }

        public void CreateXmlTransferCases()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"TransferCases-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAssociation");

                string dataFilePath = _configuration.DataFilePathJobTransfer;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0; int actionIndex = 0;int unionCodeIndex = 0; int entryDateIndex = 0;
                int numberOfRecordsCreatedInXml = 0;int actionDateIndex = 0; int deptIdPriorIndex = 0;
                int jobCodeIndex = 0;  int empIdIndex = 0;int  numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        empIdIndex = Array.IndexOf(header, "ID");                        
                        actionDateIndex = Array.IndexOf(header, "Effective Date");
                        actionIndex = Array.IndexOf(header, "Action");
                        deptIdPriorIndex = Array.IndexOf(header, "DeptID Prior");                     
                        entryDateIndex = Array.IndexOf(header, "Job Entry Date Prior");
                        unionCodeIndex = Array.IndexOf(header, "Union Code Prior");                       
                        jobCodeIndex = Array.IndexOf(header, "Job Code Prior");                       


                        if (actionIndex < 0 || empIdIndex < 0 || entryDateIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains the ID or JobCode or ActionDt headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var staffAssociationData = new StaffAssociationData
                        {
                            staffId = fields[empIdIndex]?.Trim(),
                            deptId = fields[deptIdPriorIndex]?.Trim(),
                            action = fields[actionIndex]?.Trim(),                            
                            endDate = fields[actionDateIndex]?.Trim(),
                            jobCode = fields[jobCodeIndex]?.Trim(),                            
                            entryDate = fields[entryDateIndex]?.Trim(),
                            unionCode = fields[unionCodeIndex]?.Trim(),                           

                        };

                        string descCode = Constants.StaffClassificationDescriptorCode(staffAssociationData.jobCode, int.Parse(staffAssociationData.deptId), staffAssociationData.unionCode).ToString().Trim();
                        _log.Debug($"Creating node for {staffAssociationData.staffId}-{staffAssociationData.deptId}-{staffAssociationData.endDate}");
                        CreateNodeTransferCases(staffAssociationData, descCode,writer);
                        numberOfRecordsCreatedInXml++;                        
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    _log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    _log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                _log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating JobCode XML , Exception: {ex.Message}");
            }
        }

        public void CreateXmlStaffContact()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffContacts-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAssociation");

                string dataFilePath = _configuration.DataFilePathStaffPhoneNumbers;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0; int extIndex = 0; int numberOfRecordsCreatedInXml = 0; int phoneIndex = 0;
                int preferredIndex = 0; int empIdIndex = 0; int numberOfRecordsSkipped = 0; int typeIndex = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        empIdIndex = Array.IndexOf(header, "ID");
                        phoneIndex = Array.IndexOf(header, "Phone");
                        typeIndex = Array.IndexOf(header, "Type");
                        extIndex = Array.IndexOf(header, "Ext");
                        preferredIndex = Array.IndexOf(header, "Preferred");
                        if (empIdIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains the ID or JobCode or ActionDt headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var staffContactData = new StaffContactData
                        {
                            Id = fields[empIdIndex]?.Trim(),                           
                            TelephoneNumber = fields[phoneIndex]?.Trim(),
                            TelephoneNumberTypeDescriptor = fields[typeIndex]?.Trim(),
                            Ext = fields[extIndex]?.Trim(),
                            OrderOfPriority = fields[preferredIndex]?.Trim(),
                            TextMessageCapabilityIndicator = true
                        };

                       
                        _log.Debug($"Creating node for {staffContactData.Id}-{staffContactData.TelephoneNumber}-{staffContactData.TelephoneNumberTypeDescriptor}");
                        if (!string.IsNullOrEmpty(staffContactData.TelephoneNumberTypeDescriptor))
                        {
                            var telPhoneType = Constants.GetTelephoneType(staffContactData.TelephoneNumberTypeDescriptor);
                            staffContactData.TelephoneNumberTypeDescriptor = telPhoneType;
                        }
                        if (!string.IsNullOrEmpty(staffContactData.TelephoneNumberTypeDescriptor))
                        {
                            var preNum = Constants.GetPreferredNumber(staffContactData.OrderOfPriority);
                            staffContactData.OrderOfPriority = preNum;
                        }
                        CreateNodeStaffContact(staffContactData, writer);
                        numberOfRecordsCreatedInXml++;
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    _log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    _log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                _log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating JobCode XML , Exception: {ex.Message}");
            }
        }
        private  XmlNodeList GetDeptforLocation()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigurationManager.AppSettings["XMLOutputPath"] + $"/EducationOrganization-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlNodeList list = xmlDoc.SelectNodes(@"//InterchangeEducationOrganization/EducationServiceCenter");
                return list;
            }

            catch (Exception ex)
            {
                _log.Error($"Error accessing the Dept File from peoplesoft , Exception: {ex.Message}");
                return null;
            }

        }
        private void CreateNodeJob(StaffAssociationData staffData, string descCode, string empCode, string jobIndicator, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for jobcode:{staffData.deptId} and EntryDate:{staffData.entryDate}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");

                writer.WriteStartElement("StaffReference");
                writer.WriteStartElement("StaffIdentity");

                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.staffId);
                writer.WriteEndElement();

                writer.WriteStartElement("FirstName");
                writer.WriteString(staffData.firstName);
                writer.WriteEndElement();
                writer.WriteStartElement("MiddleName");
                writer.WriteString(staffData.middleName);
                writer.WriteEndElement();
                writer.WriteStartElement("LastName");
                writer.WriteString(staffData.lastName);
                writer.WriteEndElement();
                writer.WriteStartElement("BirthDate");
                writer.WriteString(staffData.birthDate);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationReference");

                writer.WriteStartElement("EducationOrganizationIdentity");
                writer.WriteStartElement("EducationOrganizationId");
                writer.WriteString(staffData.deptId);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("EmploymentStatus");
                writer.WriteStartElement("CodeValue");
                writer.WriteString(empCode);
                writer.WriteEndElement();
                writer.WriteEndElement();


                writer.WriteStartElement("StaffClassification");
                writer.WriteStartElement("CodeValue");
                writer.WriteString(descCode);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("EmploymentPeriod");

                writer.WriteStartElement("Status");
                writer.WriteString(staffData.status);
                writer.WriteEndElement();

                writer.WriteStartElement("HireDate");
                writer.WriteString(staffData.hireDate);
                writer.WriteEndElement();

                writer.WriteStartElement("BeginDate");
                writer.WriteString(staffData.entryDate);
                writer.WriteEndElement();

                writer.WriteStartElement("ActionDate");
                writer.WriteString(staffData.actionDate);
                writer.WriteEndElement();

                writer.WriteStartElement("PostionTitle");
                writer.WriteString(staffData.jobCode + "-" + staffData.jobTitle);
                writer.WriteEndElement();

                writer.WriteStartElement("OrderOfAssignment");
                writer.WriteString(jobIndicator);
                writer.WriteEndElement();

                writer.WriteStartElement("EndDate");
                if (staffData.status.Equals("D") || staffData.status.Equals("R") || staffData.status.Equals("T"))
                    writer.WriteString(staffData.endDate);
                else
                    writer.WriteString(null);

                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for jobcode:{staffData.deptId} and EndDate:{staffData.endDate}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for jobcode:{staffData.deptId} and EndDate:{staffData.endDate}, Exception  :{ex.Message}");
            }
        }

        private void CreateNodeTransferCases(StaffAssociationData staffData, string descCode, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for jobcode:{staffData.deptId} and EntryDate:{staffData.entryDate}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");

                writer.WriteStartElement("StaffReference");
                writer.WriteStartElement("StaffIdentity");

                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.staffId);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationReference");

                writer.WriteStartElement("EducationOrganizationIdentity");
                writer.WriteStartElement("EducationOrganizationId");
                writer.WriteString(staffData.deptId);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();


                writer.WriteStartElement("StaffClassification");
                writer.WriteStartElement("CodeValue");
                writer.WriteString(descCode);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("EmploymentPeriod");

                writer.WriteStartElement("BeginDate");
                writer.WriteString(staffData.entryDate);
                writer.WriteEndElement();

                writer.WriteStartElement("Action");
                writer.WriteString(staffData.action);
                writer.WriteEndElement();             


                writer.WriteStartElement("EndDate");
                writer.WriteString(staffData.endDate);
                
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for jobcode:{staffData.deptId} and EndDate:{staffData.endDate}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for jobcode:{staffData.deptId} and EndDate:{staffData.endDate}, Exception  :{ex.Message}");
            }
        }
        private void CreateNodeStaffContact(StaffContactData staffData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for Staff:{staffData.Id} and Work:{staffData.TelephoneNumber}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");
                writer.WriteStartElement("ContactDetails");
                
                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.Id);
                writer.WriteEndElement();
                
                writer.WriteStartElement("Phone");
                writer.WriteString(staffData.TelephoneNumber);
                writer.WriteEndElement();

                writer.WriteStartElement("Type");
                writer.WriteString(@"uri://ed-fi.org/TelephoneNumberTypeDescriptor#" + staffData.TelephoneNumberTypeDescriptor);
                writer.WriteEndElement();

                writer.WriteStartElement("Ext");
                writer.WriteString(staffData.Ext);
                writer.WriteEndElement();

                writer.WriteStartElement("Preferred");
                writer.WriteString(staffData.OrderOfPriority);
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for Contact:{staffData.Id} and Phone:{staffData.TelephoneNumber}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{staffData.Id} and Phone:{staffData.TelephoneNumber}, Exception  :{ex.Message}");
            }
        }

        private void CreateNode(string deptId, string deptTitle, string location, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for DeptId:{deptId} and DeptTitle:{deptTitle}");
                writer.WriteStartElement("EducationServiceCenter");

                writer.WriteStartElement("StateOrganizationId");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationIdentificationCode");
                writer.WriteStartElement("IdentificationCode");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationIdentificationSystem");
                writer.WriteStartElement("CodeValue");
                writer.WriteString("School");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("NameOfInstitution");
                writer.WriteString(deptTitle);
                writer.WriteEndElement();

                writer.WriteStartElement("Location");
                writer.WriteString(location);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationCategory");
                writer.WriteString("Education Service Center");
                writer.WriteEndElement();

                writer.WriteStartElement("Address");
                writer.WriteStartElement("StreetNumberName");
                writer.WriteString("2300 Washington St");
                writer.WriteEndElement();

                writer.WriteStartElement("City");
                writer.WriteString("Roxbury");
                writer.WriteEndElement();

                writer.WriteStartElement("StateAbbreviation");
                writer.WriteString("MA");
                writer.WriteEndElement();

                writer.WriteStartElement("PostalCode");
                writer.WriteString("02119");
                writer.WriteEndElement();

                writer.WriteStartElement("AddressType");
                writer.WriteString("Physical");
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("EducationServiceCenterId");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for DeptId:{deptId} and DeptTitle:{deptTitle}");
            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for DeptId:{deptId} and Dept title: {deptTitle}, Exception  :{ex.Message}");
            }
        }
        public void Archive(EdorgConfiguration _configuration)
        {
            try
            {
                _log.Info("Archiving started");
                MoveFiles(_configuration);
                DeleteOldFiles(_configuration);
                _log.Info("Archiving ended");
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

        private void CreateXmlGenericStart(XmlTextWriter writer)
        {
            try
            {
                _log.Info("CreateXML started");
                writer.WriteStartDocument();
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating Generic XML start, Exception: {ex.Message}");
            }
        }

        private void CreateXmlGenericEnd(XmlTextWriter writer, int numberOfRecordsCreatedInXml, int numberOfRecordsSkipped)
        {
            try
            {
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    _log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    _log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                _log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Error while creating Generic XML End, Exception: {ex.Message}");
            }


        }
        private void MoveFiles(EdorgConfiguration configuration)
        {
            try
            {
                string rootFolderPath = configuration.XMLOutputPath;
                string backupPath = Path.Combine(rootFolderPath, "Backup");
                string filesToDelete = @"*.xml";   // Only delete WAV files ending by "_DONE" in their filenames
                string[] fileList = System.IO.Directory.GetFiles(rootFolderPath, filesToDelete);
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                foreach (string file in fileList)
                {
                    string fileToMove = Path.Combine(rootFolderPath, Path.GetFileName(file));
                    string moveTo = Path.Combine(backupPath, Path.GetFileName(file));
                    if (File.Exists(moveTo))
                    {
                        File.Delete(moveTo);
                    }
                    File.Move(fileToMove, moveTo);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }
        private void DeleteOldFiles(EdorgConfiguration configuration)
        {
            try
            {
                string backupPath = Path.Combine(configuration.XMLOutputPath, "Backup");
                string[] files = Directory.GetFiles(backupPath);
                int numberOfBackupDays = Convert.ToInt16(ConfigurationManager.AppSettings.Get("BackupDays"));
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);

                    if (fi.LastAccessTime < DateTime.Now.AddDays(-1 * numberOfBackupDays))
                    {
                        fi.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public XmlDocument ToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        /// <summary>
        /// Loading the generated Xml to get required values
        /// </summary>
        /// <returns></returns>

        public XmlDocument LoadXml(string xmlFileName)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigurationManager.AppSettings["XMLOutputPath"] + $"/"+ xmlFileName + $"-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                return xmlDoc;

            }
            catch (Exception ex)
            {
                _log.Error("Error occured while fetching the generated xml, please check if xml file exists" + ex.Message);
                return null;
            }

        }
    }
}
