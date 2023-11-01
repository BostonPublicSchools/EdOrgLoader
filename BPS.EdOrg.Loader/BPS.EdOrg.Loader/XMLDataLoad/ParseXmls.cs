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
                writer.WriteStartElement("StaffEducationOrganizationAssociation");           

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

        public void CreateXmlStaffEmail()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffEmailPersonal-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffEmailAssociation");

                string dataFilePathStaffEmail = _configuration.DataFilePathStaffEmail;
                string[] lines = File.ReadAllLines(dataFilePathStaffEmail);

                int i = 0;
                int staffIdIndex = 0;
                int emailTypeIndex = 0;
                int emailAddressIndex = 0;
                int emailPreferredIndex = 0;
                int numberOfRecordsCreatedInXml = 0, numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        staffIdIndex = Array.IndexOf(header, "ID");
                        emailTypeIndex = Array.IndexOf(header, "Type");
                        emailAddressIndex = Array.IndexOf(header, "Email");
                        emailPreferredIndex = Array.IndexOf(header, "Preferred");
                        if (staffIdIndex < 0 || emailAddressIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains the StaffID or Staff Email Address");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var staffEmailData = new StaffElectronicMailsData
                        {
                            Id = fields[staffIdIndex]?.Trim(),
                            electronicMailTypeDescriptor = fields[emailTypeIndex]?.Trim(),
                            electronicMailAddress = fields[emailAddressIndex]?.Trim(),
                            primaryEmailAddressIndicator = Constants.GetBoolIndicator(fields[emailPreferredIndex]?.Trim())
                        };


                        _log.Debug($"Creating node for {staffEmailData.Id}-{staffEmailData.electronicMailAddress}-{staffEmailData.electronicMailTypeDescriptor}-{staffEmailData.primaryEmailAddressIndicator}");
                        CreateNodeStaffEmail(staffEmailData, writer);
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
                _log.Error($"Error while creating Dept XML , Exception: {ex.Message}");
            }
        }
        
        private void CreateNodeStaffEmail(StaffElectronicMailsData staffData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for Staff:{staffData.Id} and Email:{staffData.electronicMailAddress}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");
                writer.WriteStartElement("StaffPersonalEmail");

                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.Id);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffEmail");
                writer.WriteString(staffData.electronicMailAddress);
                writer.WriteEndElement();

                writer.WriteStartElement("Type");
                writer.WriteString("uri://ed-fi.org/TelephoneNumberTypeDescriptor#" + staffData.electronicMailTypeDescriptor);
                writer.WriteEndElement();

                writer.WriteStartElement("EmailAddressIndicator");
                writer.WriteString(staffData.primaryEmailAddressIndicator.ToString());
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for Contact:{staffData.Id} and Phone:{staffData.electronicMailAddress}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{staffData.Id} and Phone:{staffData.electronicMailAddress}, Exception  :{ex.Message}");
            }
        }

        public void CreateXmlStaffAddress()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffAddressEmployee-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAddressAssociation");
                int numberOfRecordsSkipped = 0;
                int numberOfRecordsCreatedInXml = 0;
                List<string> DataStaffAB = new List<string>();
                string[] DataFilePathStaffAddressEmployees = File.ReadAllLines(_configuration.DataFilePathStaffAddressEmployees);
                string[] DataFilePathStaffAddressA = File.ReadAllLines(_configuration.DataFilePathStaffAddressA);
                string[] DataFilePathStaffAddressB = File.ReadAllLines(_configuration.DataFilePathStaffAddressB).Skip(1).ToArray();

                DataStaffAB.AddRange(DataFilePathStaffAddressA.ToList());
                DataStaffAB.AddRange(DataFilePathStaffAddressB.ToList());

                List<StaffAddressData> staffAddressData = GetStaffAddressDataEmployee(DataFilePathStaffAddressEmployees, writer);
                List<StaffAddressData> staffAddressDataAB = GetStaffAddressDataEmployeeAB(DataStaffAB, writer);
                staffAddressData.AddRange(staffAddressDataAB);

                foreach ( var data in staffAddressData)
                {
                    _log.Debug($"Creating node for {data.Id}-{data.streetNumberName}-{data.city}-{data.postalCode}");
                    CreateNodeStaffAddress(data, writer);
                    numberOfRecordsCreatedInXml++;
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

        private List<StaffAddressData> GetStaffAddressDataEmployee(string[] DataFilePathStaffAddressEmployees, XmlTextWriter writer)
        {
            int i = 0;
            int staffIdIndex = 0;
            int staffAddressIndex = 0;            
            int staffCityIndex = 0;
            int staffStateIndex = 0;
            int staffZipIndex = 0;
            List<StaffAddressData> staffAddressData = new List<StaffAddressData>();
            StaffAddressData staffAddress = null;
            foreach (string line in DataFilePathStaffAddressEmployees)
            {
                _log.Debug(line);
                if (i++ == 0)
                {
                    string[] header = line.Split('\t');
                    staffIdIndex = Array.IndexOf(header, "ID");                    
                    staffAddressIndex = Array.IndexOf(header, "Address 1");
                    staffCityIndex = Array.IndexOf(header, "City");
                    staffStateIndex = Array.IndexOf(header, "St");
                    staffZipIndex = Array.IndexOf(header, "Zip");
                    if (staffIdIndex < 0 || staffAddressIndex < 0 || staffStateIndex < 0)
                    {
                        _log.Error($"Input data text file does not contains the StaffID or StaffAddress");
                    }
                    continue;
                }

                string[] fields = line.Split('\t');
                if (fields.Length > 0)
                {
                    staffAddress = new StaffAddressData
                    {
                        Id = fields[staffIdIndex]?.Trim(),                       
                        streetNumberName = fields[staffAddressIndex]?.Trim(),
                        city = fields[staffCityIndex]?.Trim(),
                        stateAbbreviationDescriptor = fields[staffStateIndex]?.Trim(),
                        postalCode = fields[staffZipIndex]?.Trim()
                    };
                    staffAddressData.Add(staffAddress);
                }
                
            }
            return staffAddressData;
        }

        private List<StaffAddressData> GetStaffAddressDataEmployeeAB(List<string> DataFilePathStaffAddressEmployees, XmlTextWriter writer)
        {
            int i = 0;
            int staffIdIndex = 0;
            int AddrType = 0;
            int staffAddressIndexA = 0;
            int staffAddressIndexB = 0;
            int staffCityIndex = 0;
            int staffStateIndex = 0;
            int staffZipIndex = 0;
            List<StaffAddressData> staffAddressData = new List<StaffAddressData>();
            StaffAddressData staffAddress = null;
            foreach (string line in DataFilePathStaffAddressEmployees)
            {
                _log.Debug(line);
                if (i++ == 0)
                {
                    string[] header = line.Split('\t');
                    staffIdIndex = Array.IndexOf(header, "ID");
                    AddrType = Array.IndexOf(header, "Addr Type");
                    staffAddressIndexA = Array.IndexOf(header, "Address 1");
                    staffAddressIndexB = Array.IndexOf(header, "Address 2");
                    staffCityIndex = Array.IndexOf(header, "City");
                    staffStateIndex = Array.IndexOf(header, "State");
                    staffZipIndex = Array.IndexOf(header, "Postal");
                    if (staffIdIndex < 0 || staffAddressIndexA < 0 || staffStateIndex < 0)
                    {
                        _log.Error($"Input data text file does not contains the StaffID or StaffAddress");
                    }
                    continue;
                }

                string[] fields = line.Split('\t');
                if (fields.Length > 0)
                {
                    staffAddress = new StaffAddressData
                    {
                        Id = fields[staffIdIndex]?.Trim(),
                        addressTypeDescriptor = fields[AddrType]?.Trim(),
                        streetNumberName = fields[staffAddressIndexA]+ fields[staffAddressIndexB]?.Trim(),
                        city = fields[staffCityIndex]?.Trim(),
                        stateAbbreviationDescriptor = fields[staffStateIndex]?.Trim(),
                        postalCode = fields[staffZipIndex]?.Trim()
                    };

                    staffAddressData.Add(staffAddress);

                }

            }
            return staffAddressData;
        }

        private  void CreateNodeStaffAddress(StaffAddressData staffData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for StaffAddressData:{staffData.Id} and Email:{staffData.streetNumberName}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");
                writer.WriteStartElement("StaffAddress");

                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.Id);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffAddressType");
                writer.WriteString("uri://ed-fi.org/AddressTypeDescriptor#" + Constants.GetAddressDescriptor(staffData.addressTypeDescriptor));
                writer.WriteEndElement();

                writer.WriteStartElement("StaffAddress");
                writer.WriteString(staffData.streetNumberName);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffCity");
                writer.WriteString(staffData.city);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffState");
                writer.WriteString("uri://ed-fi.org/StateAbbreviationDescriptor#" + staffData.stateAbbreviationDescriptor);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffZip");
                writer.WriteString(staffData.postalCode);
                writer.WriteEndElement();

                writer.WriteStartElement("StaffLocale");
                writer.WriteString("uri://ed-fi.org/LocaleDescriptor#City-Large");
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for Contact:{staffData.Id} and Phone:{staffData.streetNumberName}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{staffData.Id} and Phone:{staffData.streetNumberName}, Exception  :{ex.Message}");
            }
        }


        public void CreateXmlJob()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffAssociation-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAssociation");
               
                // Comapring Previous and current files
                string dataFilePathPreviousFile = _configuration.DataFilePathJobPreviousFile;
                string dataFilePathCurrentFile = _configuration.DataFilePathJob;
                string dataFilePath = _configuration.DataFilePathJob;
                string[] previousFileLines = null;
                if (dataFilePathPreviousFile != null)
                    previousFileLines = File.ReadAllLines(dataFilePathPreviousFile).Skip(1).ToArray();
                string[] currentFileLines = File.ReadAllLines(dataFilePathCurrentFile, System.Text.Encoding.GetEncoding("iso-8859-1")); // parsing the special characters
                // New records that need to be Updated or Inserted
                IEnumerable<String> lines = currentFileLines.Except(previousFileLines);
                

                // Creating xml for only the currnet fields
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


                        if (deptIdIndex < 0 || actionIndex < 0 || empIdIndex < 0 || hireDateIndex < 0 || entryDateIndex < 0 || unionCodeIndex <0 || firstNameIndex < 0 || lastNameIndex < 0)
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
                            var deptIdLocation = nodeList.Cast<XmlNode>().Where(n => n["Location"].InnerText == staffAssociationData.location).Select(x => x["EducationServiceCenterId"].InnerText).FirstOrDefault();
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
                            telephoneNumber = fields[phoneIndex]?.Trim(),
                            telephoneNumberTypeDescriptor = fields[typeIndex]?.Trim(),
                            ext = fields[extIndex]?.Trim(),
                            orderOfPriority = fields[preferredIndex]?.Trim(),
                            textMessageCapabilityIndicator = true
                        };

                       
                        _log.Debug($"Creating node for {staffContactData.Id}-{staffContactData.telephoneNumber}-{staffContactData.telephoneNumberTypeDescriptor}");
                        if (!string.IsNullOrEmpty(staffContactData.telephoneNumberTypeDescriptor))
                        {
                            var telPhoneType = Constants.GetTelephoneType(staffContactData.telephoneNumberTypeDescriptor);
                            staffContactData.telephoneNumberTypeDescriptor = telPhoneType;
                        }
                        if (!string.IsNullOrEmpty(staffContactData.telephoneNumberTypeDescriptor))
                        {
                            var preNum = Constants.GetPreferredNumber(staffContactData.orderOfPriority);
                            staffContactData.orderOfPriority = preNum;
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

        public void CreateXmlEdPlanToAspenTxt()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLExtractedPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StudentEdPlantoAspen-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("root");

                string dataFilePath = _configuration.DataFilePathEdPlantoApen;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0; int nextEvalDateIndex = 0; int numberOfRecordsCreatedInXml = 0; int iepBeginDateIndex = 0; int iepEndDateIndex = 0;
                int iepReviewDateIndex = 0; int spEdExitDateIndex = 0; int dateSignedIndex = 0; int agencyIndex = 0; int costShareIndex = 0;
                int programIndex = 0; int placementSchoolIndex = 0; int numberOfRecordsSkipped = 0; int parentResponseIndex = 0; int studentNumberIndex = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        studentNumberIndex = Array.IndexOf(header, "Student Number");
                        placementSchoolIndex = Array.IndexOf(header, "Placement School");
                        programIndex = Array.IndexOf(header, "Program");
                        iepBeginDateIndex = Array.IndexOf(header, "Current IEP Begin Date");
                        iepEndDateIndex = Array.IndexOf(header, "Current IEP End Date");
                        iepReviewDateIndex = Array.IndexOf(header, "Next Review Date");
                        spEdExitDateIndex = Array.IndexOf(header, "Special Education Exit Date");
                        nextEvalDateIndex = Array.IndexOf(header, "Next Evaluation Date");
                        parentResponseIndex = Array.IndexOf(header, "Parent Response");
                        dateSignedIndex = Array.IndexOf(header, "Date Signed");
                        agencyIndex = Array.IndexOf(header, "Agency");
                        costShareIndex = Array.IndexOf(header, "Cost Share");
                        if (studentNumberIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains StudentNumber headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var EdPlanToAspenTxt = new EdPlanToAspenTxt
                        {
                            studentNumber = fields[studentNumberIndex]?.Trim(),
                            placementSchool = fields[placementSchoolIndex]?.Trim(),
                            program = fields[programIndex]?.Trim(),
                            iepBeginDate = fields[iepBeginDateIndex]?.Trim(),
                            iepEndDate = fields[iepEndDateIndex]?.Trim(),
                            iepReviewDate = fields[iepReviewDateIndex]?.Trim(),
                            spEdExitDate = fields[spEdExitDateIndex]?.Trim(),
                            nextEvalDate = fields[nextEvalDateIndex]?.Trim(),
                            parentResponse = fields[parentResponseIndex]?.Trim(),
                            dateSigned = fields[dateSignedIndex]?.Trim(),
                            agency = fields[agencyIndex]?.Trim(),
                            costShare = fields[costShareIndex]?.Trim(),
                        };


                        _log.Debug($"Creating node for {EdPlanToAspenTxt.studentNumber}-{EdPlanToAspenTxt.placementSchool}-{EdPlanToAspenTxt.program}");
                        CreateNodeEdPlantoAspenTxt(EdPlanToAspenTxt, writer);
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

        public void CreateXmlSpedSimsTxt()
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"SpedSims-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("root");

                string dataFilePath = _configuration.DataFilePathSpedSims;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0;  int studentNumberIndex = 0; int disabilityDescIndex = 0; int levelNeedInfoIndex = 0; int numberOfRecordsCreatedInXml = 0;
                int numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    _log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        studentNumberIndex = Array.IndexOf(header, "DOE001");
                        disabilityDescIndex = Array.IndexOf(header, "DOE036");
                        levelNeedInfoIndex = Array.IndexOf(header, "DOE038");
                        
                        if (studentNumberIndex < 0)
                        {
                            _log.Error($"Input data text file does not contains StudentNumber headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        var SpedSimsTxt = new SpedSimsTxt
                        {
                            studentNumber = fields[studentNumberIndex]?.Trim(),
                            disabilityInfo = fields[disabilityDescIndex]?.Trim(),
                            levelNeedInfo = fields[levelNeedInfoIndex]?.Trim()
                            
                        };


                        _log.Debug($"Creating node for {SpedSimsTxt.studentNumber}-{SpedSimsTxt.disabilityInfo}-{SpedSimsTxt.levelNeedInfo}");
                        if(!SpedSimsTxt.disabilityInfo.Equals("500") || !SpedSimsTxt.levelNeedInfo.Equals("500"))
                        CreateNodeSpedSimsTxt(SpedSimsTxt, writer);
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
                XmlNodeList list = xmlDoc.SelectNodes(@"//StaffEducationOrganizationAssociation/EducationServiceCenter");
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
                writer.WriteStartElement("UnionCode");
                if(staffData.jobIndicator.Equals("P"))
                writer.WriteString(staffData.unionCode);
                else
                    writer.WriteString(null);
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

        private void CreateNodeEdPlantoAspenTxt(EdPlanToAspenTxt IepData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for Student:{IepData.studentNumber} and Placement:{IepData.placementSchool}");
               
                writer.WriteStartElement("iep");

                writer.WriteStartElement("educationOrganizationReference");

                writer.WriteStartElement("educationOrganizationId");
                writer.WriteString("350000");
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("programReference");

                writer.WriteStartElement("educationOrganizationId");
                writer.WriteString(IepData.placementSchool);
                writer.WriteEndElement();

                writer.WriteStartElement("type");
                writer.WriteString("Special Education");
                writer.WriteEndElement();

                writer.WriteStartElement("name");
                writer.WriteString(IepData.program);
                writer.WriteEndElement();


                writer.WriteEndElement();

                writer.WriteStartElement("studentReference");

                writer.WriteStartElement("studentUniqueId");
                writer.WriteString(IepData.studentNumber);
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("iepBeginDate");
                writer.WriteString(IepData.iepBeginDate);
                writer.WriteEndElement();

                writer.WriteStartElement("iepEndDate");
                writer.WriteString(IepData.iepEndDate);
                writer.WriteEndElement();

                writer.WriteStartElement("iepReviewDate");
                writer.WriteString(IepData.iepReviewDate);
                writer.WriteEndElement();

                writer.WriteStartElement("exitDate");
                writer.WriteString(IepData.spEdExitDate);
                writer.WriteEndElement();

                writer.WriteStartElement("lastEvaluationDate");
                writer.WriteString(IepData.nextEvalDate);
                writer.WriteEndElement();

                writer.WriteStartElement("parentResponse");
                writer.WriteString(IepData.parentResponse);
                writer.WriteEndElement();

                writer.WriteStartElement("dateSigned");
                writer.WriteString(IepData.dateSigned);
                writer.WriteEndElement();

                writer.WriteStartElement("Agency");
                writer.WriteString(IepData.agency);
                writer.WriteEndElement();

                writer.WriteStartElement("CostShare");
                writer.WriteString(IepData.costShare);
                writer.WriteEndElement();

                writer.WriteStartElement("DataSource");
                writer.WriteString("Txt");
                writer.WriteEndElement();

                writer.WriteEndElement();                

                _log.Info($"CreateNode Ended successfully for Contact:{IepData.studentNumber} and Phone:{IepData.program}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{IepData.studentNumber} and Phone:{IepData.program}, Exception  :{ex.Message}");
            }
        }

        private void CreateNodeSpedSimsTxt(SpedSimsTxt IepData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for Student:{IepData.studentNumber} and DisabilityDescriptor:{IepData.disabilityInfo}");

                writer.WriteStartElement("iep");

                writer.WriteStartElement("studentNumber");
                writer.WriteString(IepData.studentNumber);
                writer.WriteEndElement();

                writer.WriteStartElement("disabilityDesc");
                writer.WriteString(IepData.disabilityInfo);
                writer.WriteEndElement();

                writer.WriteStartElement("levelNeed");
                writer.WriteString(IepData.levelNeedInfo);
                writer.WriteEndElement();


                writer.WriteEndElement();              

                _log.Info($"CreateNode Ended successfully for Contact:{IepData.studentNumber} and Phone:{IepData.disabilityInfo}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{IepData.studentNumber} and Phone:{IepData.disabilityInfo}, Exception  :{ex.Message}");
            }
        }
        private void CreateNodeStaffContact(StaffContactData staffData, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for Staff:{staffData.Id} and Work:{staffData.telephoneNumber}");
                writer.WriteStartElement("StaffEducationOrganizationAssociation");
                writer.WriteStartElement("ContactDetails");
                
                writer.WriteStartElement("StaffUniqueId");
                writer.WriteString(staffData.Id);
                writer.WriteEndElement();
                
                writer.WriteStartElement("Phone");
                writer.WriteString(staffData.telephoneNumber);
                writer.WriteEndElement();

                writer.WriteStartElement("Type");
                writer.WriteString("uri://ed-fi.org/TelephoneNumberTypeDescriptor#" + staffData.telephoneNumberTypeDescriptor);
                writer.WriteEndElement();

                writer.WriteStartElement("Ext");
                writer.WriteString(staffData.ext);
                writer.WriteEndElement();

                writer.WriteStartElement("Preferred");
                writer.WriteString(staffData.orderOfPriority);
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndElement();

                _log.Info($"CreateNode Ended successfully for Contact:{staffData.Id} and Phone:{staffData.telephoneNumber}");

            }
            catch (Exception ex)
            {
                _log.Error($"There is exception while creating Node for Contact:{staffData.Id} and Phone:{staffData.telephoneNumber}, Exception  :{ex.Message}");
            }
        }

        private void CreateNode(string deptId, string deptTitle, string location, XmlTextWriter writer)
        {
            try
            {
                _log.Info($"CreateNode started for DeptId:{deptId} and DeptTitle:{deptTitle}");
                writer.WriteStartElement("EducationServiceCenter");

                writer.WriteStartElement("EducationOrganizationIdentificationCode");
                writer.WriteStartElement("IdentificationCode");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteStartElement("educationOrganizationIdentificationSystemDescriptor");
                writer.WriteString("uri://ed-fi.org/EducationOrganizationIdentificationSystemDescriptor#School");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("NameOfInstitution");
                writer.WriteString(deptTitle);
                writer.WriteEndElement();

                writer.WriteStartElement("Location");
                writer.WriteString(location);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationCategoryDescriptor");
                writer.WriteString("uri://ed-fi.org/EducationOrganizationCategoryDescriptor#Education Service Center");
                writer.WriteEndElement();

                writer.WriteStartElement("Address");
                writer.WriteStartElement("StreetNumberName");
                writer.WriteString("2300 Washington St");
                writer.WriteEndElement();

                writer.WriteStartElement("City");
                writer.WriteString("Roxbury");
                writer.WriteEndElement();

                writer.WriteStartElement("StateAbbreviationDescriptor");
                writer.WriteString("uri://ed-fi.org/StateAbbreviationDescriptor#MA");
                writer.WriteEndElement();

                writer.WriteStartElement("PostalCode");
                writer.WriteString("02119");
                writer.WriteEndElement();

                writer.WriteStartElement("AddressTypeDescriptor");
                writer.WriteString("uri://ed-fi.org/AddressTypeDescriptor#Physical");
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
                string rootFolderPath = configuration.JobFilePath;                
                string backupPath = Path.Combine(configuration.XMLOutputPath, "Backup");

                List<string> fileList = new List<string>();

                fileList.AddRange(Directory.GetFiles(rootFolderPath, Constants.JobFile)); // Move Job file for comparison
                fileList.AddRange(Directory.GetFiles(rootFolderPath, Constants.JobEdPlanTxtFile)); // Archive Txt file
                fileList.AddRange(Directory.GetFiles(ConfigurationManager.AppSettings["XMLExtractedPath"], Constants.JobEdPlanXmlFile)) ;// Move Job file for comparison



                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                foreach (string file in fileList)
                {
                    string fileToMove = Path.Combine(rootFolderPath, Path.GetFileName(file)); // Get the filepath and file
                    string moveTo = Path.Combine(backupPath, Path.GetFileName(file)); // Destination filepath
                    if (File.Exists(moveTo))
                    {
                        FileInfo fi = new FileInfo(file);
                        //Archiving files only for 7 days
                        //if (fi.LastAccessTime< DateTime.Now.AddDays(-7))
                            File.Delete(moveTo);
                    }
                    File.Copy(fileToMove, moveTo,true);
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
                string outputPath = configuration.XMLOutputPath;
                string[] files = Directory.GetFiles(outputPath);
                int numberOfBackupDays = Convert.ToInt16(ConfigurationManager.AppSettings.Get("BackupDays"));
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);

                    if (fi.CreationTime < DateTime.Now.AddDays(-1 * numberOfBackupDays))
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
                xmlDoc.Load(ConfigurationManager.AppSettings["XMLOutputPath"] + $"/" + xmlFileName + $"-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
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
