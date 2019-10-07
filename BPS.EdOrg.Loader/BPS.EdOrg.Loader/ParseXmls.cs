using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Linq;

namespace BPS.EdOrg.Loader
{
    class ParseXmls
    {
        private readonly Configuration _configuration = null;      
        private readonly ILog _log;
        public ParseXmls(Configuration configuration, ILog logger)
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
                //writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                //writer.WriteAttributeString("xmlns", "ann", null, "http://ed-fi.org/annotation");
                //writer.WriteAttributeString("xmlns", null, "http://ed-fi.org/0220");

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
                        //if (!existingDeptIds.Any(p => p.DeptId == deptId))
                        //{
                        //    Log.Debug($"Creating node for {deptId}-{deptTitle}");
                        //    CreateNode(deptId, deptTitle, writer);
                        //    numberOfRecordsCreatedInXml++;
                        //}
                        //else
                        //{
                        //    Log.Debug($"Record skipped : {line}");
                        //    numberOfRecordsSkipped++;
                        //}
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
                //writer.WriteAttributeString("xmlns", null, "http://ed-fi.org/0220");
                //writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                //writer.WriteAttributeString("xmlns", "ann", null, "http://ed-fi.org/annotation");



                string dataFilePath = _configuration.DataFilePathJob;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0;
                int deptIdIndex = 0; int unionCodeIndex = 0; int emplClassIndex = 0; int jobIndicatorIndex = 0; int statusIndex = 0;
                int actionIndex = 0; int actionDateIndex = 0; int hireDateIndex = 0; int jobCodeIndex = 0; int jobTitleIndex = 0;
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
                        actionDateIndex = Array.IndexOf(header, "Eff Date");
                        hireDateIndex = Array.IndexOf(header, "Orig Hire Date");
                        jobCodeIndex = Array.IndexOf(header, "Job Code");
                        jobTitleIndex = Array.IndexOf(header, "Job Title");
                        entryDateIndex = Array.IndexOf(header, "Entry Date");
                        unionCodeIndex = Array.IndexOf(header, "Union Code");
                        emplClassIndex = Array.IndexOf(header, "Empl Class");
                        jobIndicatorIndex = Array.IndexOf(header, "Job Indicator");
                        statusIndex = Array.IndexOf(header, "Status");
                        firstNameIndex = Array.IndexOf(header, "First Name");
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
                            var deptLoc = Constants.GetDeptMultipleLocation(staffAssociationData.location);
                            var dept = deptLoc ?? nodeList.Cast<XmlNode>().Where(n => n["Location"].InnerText == staffAssociationData.location).Select(x => x["StateOrganizationId"].InnerText).FirstOrDefault(); 
                                     
                            if (dept != null)
                            {
                                // If departments are different that means Physical location is different so  we have 2 assignments for the staff
                                if (!staffAssociationData.deptId.Equals(dept))
                                {
                                    staffAssociationData.deptId = dept;
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
                writer.WriteStartElement("LastName");
                writer.WriteString(staffData.lastName);
                writer.WriteEndElement();
                writer.WriteStartElement("BirthDate");
                writer.WriteString(staffData.birthDate);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                //writer.WriteStartElement("Department");
                //writer.WriteString(jobCode);
                //writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationReference");

                writer.WriteStartElement("EducationOrganizationIdentity");
                writer.WriteStartElement("EducationOrganizationId");
                writer.WriteString(staffData.deptId);
                writer.WriteEndElement();
                writer.WriteEndElement();

                //writer.WriteStartElement("EducationOrganizationLookup");
                //writer.WriteStartElement("EducationOrganizationIdentificationCode");
                //writer.WriteStartElement("EducationOrganizationIdentificationSystem");
                //writer.WriteStartElement("CodeValue");
                //writer.WriteString("School");
                //writer.WriteEndElement();
                //writer.WriteEndElement();
                //writer.WriteEndElement();
                //writer.WriteEndElement();

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
        public void Archive(Configuration _configuration)
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
        private void MoveFiles(Configuration configuration)
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
        private void DeleteOldFiles(Configuration configuration)
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
    }
}
