using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using BPS.EdPlanLoaderCore.XMLDataLoad;
using System.Xml;
using System.Xml.Linq;
using log4net;
using BPS.EdPlanLoaderCore.MetaData;
using BPS.EdPlanLoaderCore.Models;
using System.Globalization;
using System.Linq;
using RestSharp;
using BPS.EdPlanLoaderCore.EdFi.Api;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;

namespace BPS.EdPlanLoaderCore.Controller
{
    
    class StudentSpecialEducationController
    {
        private static Notification notification;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        Dictionary<Tuple<string, string>, EdFiStudentSpecialEducation> _CACHE_SPED_IEP_LOOKUP = null;


        /// <summary>
        /// Processes the 504 alert XML, updating or inserting SpecialEducation data in the ODS as appropriate,
        /// and sends a notification email if any errors are logged.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="prseXMl">Helper for XML parsing and conversion.</param>
        public void UpdateAlertSpecialEducationData(string token, ParseXmls prseXMl)
        {
            try
            {
                // Read and clean the 504 alert XML file
                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + "/504inXML.xml")
                                    .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                var doc = XDocument.Parse(fragments);
                XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);

                // Select all <student> nodes under <root>
                XmlNodeList nodeList = xmlDoc.SelectNodes("//root/student");
                foreach (XmlNode node in nodeList)
                {
                    var studentSpecialEducationList = GetAlertXml(node);

                    if (studentSpecialEducationList?.EducationOrganizationId != null
                        && studentSpecialEducationList.Name != null
                        && studentSpecialEducationList.Type != null)
                    {
                        // Ensure program exists in the ODS
                        VerifyProgramData(token, studentSpecialEducationList.ProgramEducationOrganizationId, studentSpecialEducationList.Name, studentSpecialEducationList.Type);

                        if (studentSpecialEducationList.StudentUniqueId != null)
                        {
                            // Insert if BeginDate is present
                            if (!string.IsNullOrEmpty(studentSpecialEducationList.BeginDate))
                            {
                                InsertAlertDataSpecialEducation(token, studentSpecialEducationList);
                            }

                            // Update if IepExitDate is present
                            if (!string.IsNullOrEmpty(studentSpecialEducationList.IepExitDate))
                            {
                                UpdateAlertStudentSpecialEducation(token, studentSpecialEducationList);
                            }
                        }
                    }
                }

                // Send error log as notification, if there is a log file
                if (File.Exists(Constants.LOG_FILE))
                {
                    notification.SendMail(
                        Constants.LOG_FILE_REC,
                        Constants.LOG_FILE_SUB,
                        Constants.LOG_FILE_BODY,
                        Constants.LOG_FILE_ATT
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }


        /// <summary>
        /// Inserts or updates SpecialEducation (504 Plan) alert data for a student in the ODS.
        /// - If a matching record exists, updates or replaces it as needed.
        /// - If not, inserts a new record.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="spList">SpecialEducation alert object to insert or update.</param>
        private static void InsertAlertDataSpecialEducation(string token, SpecialEducation spList)
        {
            IRestResponse response = null;
            try
            {
                var rootObject = GetAlertSpecialEducation(token, spList);
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine("Inside Alert SpecialED");

                // Construct the ODS GET URL for checking existing records
                var client = new RestClient(
                    ConfigurationManager.AppSettings["ApiUrl"] +
                    Constants.StudentSpecialEducation +
                    Constants.studentUniqueId + spList.StudentUniqueId +
                    "&programName=504 Plan" +
                    Constants.beginDate + spList.IepSignatureDate);

                response = edfiApi.GetData(client, token);
                var original = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);

                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        // Check each existing record for possible update or insert
                        foreach (var data in original)
                        {
                            var id = data.id;
                            string stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate = Convert.ToDateTime(data.beginDate);

                            if (id != null && spList.IepSignatureDate != null)
                            {
                                DateTime inputDateTime;
                                if (DateTime.TryParse(spList.IepSignatureDate, out inputDateTime))
                                {
                                    var result = DateTime.Compare(inputDateTime, iepDate);
                                    if (stuId == spList.StudentUniqueId && result == 0)
                                    {
                                        // Fix empty or null ProgramEducationOrganizationId
                                        if (string.IsNullOrEmpty(spList.ProgramEducationOrganizationId))
                                            spList.ProgramEducationOrganizationId = Constants.educationOrganizationIdValue;

                                        // Check for changed program org, begin date, or name
                                        bool needsUpdate =
                                            DateTime.Parse(data.beginDate, CultureInfo.InvariantCulture) != DateTime.Parse(spList.BeginDate, CultureInfo.InvariantCulture) ||
                                            !data.programReference.educationOrganizationId.Equals(spList.ProgramEducationOrganizationId.TrimStart('0')) ||
                                            !data.programReference.ProgramName.Equals(spList.Name);

                                        if (needsUpdate)
                                        {
                                            var updateResponse = edfiApi.DeleteData(
                                                new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id),
                                                token);
                                            if (IsSuccessStatusCode((int)updateResponse.StatusCode))
                                                updateResponse = edfiApi.PostData(json, client, token);

                                            Log.Info("Update StudentSpecialEdOrg : studentUniqueId " + stuId + " IepSignatureDate " + spList.IepSignatureDate);
                                        }
                                    }
                                    else
                                    {
                                        var insertResponse = edfiApi.PostData(json, client, token);
                                        Log.Info("Insert StudentSpecialEdOrg : studentUniqueId " + stuId + " IepSignatureDate " + spList.IepSignatureDate);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // No existing record, insert new
                        response = edfiApi.PostData(json, client, token);
                        Log.Info("Inserting if record doesn't exist StudentSpecialEdOrg : studentUniqueId " + spList.StudentUniqueId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values: " + ex.Message);
            }
        }

        /// <summary>
        /// Updates the EndDate for students' Special Education program associations in the ODS, 
        /// based on the provided list of current IEP students. If a student is no longer in the IEP list,
        /// their EndDate is set to today. If a student has multiple records in ODS, their EndDate is updated accordingly.
        /// </summary>
        /// <param name="specialEdPlan">Identifier or file for the Special Education plan.</param>
        /// <param name="token">Authentication token for ODS API access.</param>
        /// <param name="prseXMl">Instance of ParseXmls used for XML operations (not used in this method).</param>
        /// <param name="studentsIEP">List of studentUniqueId's who currently have an IEP.</param>
        public void UpdateEndDateSpecialEducation(string specialEdPlan, string token, ParseXmls prseXMl, List<string> studentsIEP)
        {
            try
            {
                string endDate = null;
                Dictionary<string, UpdateEndDateStudent> studentSpecialEducations = null;

                // Retrieve Special Education program associations from ODS
                studentSpecialEducations = GetStudentSpecialEducation(specialEdPlan, token);

                if (studentSpecialEducations != null && studentSpecialEducations.Any())
                {
                    if (studentsIEP != null && studentsIEP.Any())
                    {
                        // Set EndDate for students no longer found in the IEP list and whose EndDate is still null
                        var studentsNotInXml = studentSpecialEducations
                            .Where(t2 => studentsIEP.All(t1 => !t2.Value.studentReference.studentUniqueId.Equals(t1)) && (t2.Value.EndDate == null))
                            .ToList();

                        foreach (var item in studentsNotInXml)
                        {
                            endDate = DateTime.Now.ToString();
                            if (!string.IsNullOrEmpty(endDate))
                                item.Value.EndDate = endDate.Split()[0]; // Only take the date part
                            SetEndDate(specialEdPlan, token, item.Value);
                        }

                        // Set EndDate for students with multiple records in ODS
                        var studentsInXml = studentSpecialEducations
                            .GroupBy(t1 => t1)
                            .Where(t2 => t2.Count() > 1)
                            .Select(t3 => t3.Key)
                            .ToList();

                        foreach (var item in studentsInXml)
                        {
                            endDate = GetEndDateProgramAssociation(specialEdPlan, token, item.Value);
                            if (!string.IsNullOrEmpty(endDate))
                                item.Value.EndDate = endDate.Split()[0];
                            SetEndDate(specialEdPlan, token, item.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }


        /// <summary>
        /// Determines the appropriate EndDate for a student's Special Education program association
        /// by querying the ODS for program records. If the student's BeginDate is the latest, returns null.
        /// Otherwise, returns the most recent previous beginDate as the EndDate.
        /// </summary>
        /// <param name="specialEdPlan">Identifier or query string for the Special Education plan.</param>
        /// <param name="token">Authentication token for the API.</param>
        /// <param name="spList">The student's current Special Education association object.</param>
        /// <returns>The EndDate as a string (or null if no EndDate should be set).</returns>
        private static string GetEndDateProgramAssociation(string specialEdPlan, string token, UpdateEndDateStudent spList)
        {
            // Default to now, will be overwritten if logic determines otherwise
            string endDate = DateTime.Now.ToString("yyyy-MM-dd");
            try
            {
                // Build REST client with the correct API URL and parameters
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] +
                                           Constants.StudentSpecialEducation +
                                           Constants.studentUniqueId +
                                           spList.studentReference.studentUniqueId +
                                           specialEdPlan);

                // Query the ODS API for Special Education associations
                IRestResponse response = edfiApi.GetData(client, token);

                // If the response is not empty (ignore empty lists like "[]")
                if (response.Content.Length > 2)
                {
                    // Deserialize the response content into a list of references
                    List<SpecialEducationReference> data = JsonConvert.DeserializeObject<List<SpecialEducationReference>>(response.Content);

                    // If there are at least two associations, find the latest one
                    if (data.Count >= 2)
                    {
                        // Parse the current record's BeginDate
                        DateTime beginDate;
                        DateTime.TryParse(spList.BeginDate, out beginDate);

                        DateTime maxValue = default(DateTime);

                        // Find the maximum beginDate among all associations
                        foreach (var item in data)
                        {
                            DateTime inputDateTime;
                            DateTime.TryParse(item.beginDate, out inputDateTime);
                            int result = DateTime.Compare(inputDateTime, maxValue);
                            if (result >= 0)
                                maxValue = inputDateTime;
                        }

                        // If the current association's BeginDate is the latest, no EndDate is needed
                        int resultDate = DateTime.Compare(beginDate, maxValue);
                        if (resultDate >= 0)
                            endDate = null;
                        else
                            endDate = maxValue.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        endDate = null;
                    }
                }

                return endDate;
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values: " + ex.Message);
                return endDate;
            }
        }


        /// <summary>
        /// Updates the EndDate for a student's Special Education program association in the ODS.
        /// Fetches the student's current Special Education record(s) using the unique identifiers and updates the endDate field.
        /// </summary>
        /// <param name="specialEdPlan">The Special Education plan identifier or query string.</param>
        /// <param name="token">The authentication token for the API.</param>
        /// <param name="spItem">The student program association object with updated EndDate value.</param>
        private static void SetEndDate(string specialEdPlan, string token, UpdateEndDateStudent spItem)
        {
            try
            {
                // Build the API endpoint for retrieving the student's record(s)
                var client = new RestClient(
                    ConfigurationManager.AppSettings["ApiUrl"] +
                    Constants.StudentSpecialEducation +
                    Constants.studentUniqueId +
                    spItem.studentReference.studentUniqueId +
                    specialEdPlan +
                    Constants.beginDate +
                    spItem.BeginDate);

                // Get the existing Special Education associations for this student/plan/beginDate
                IRestResponse response = edfiApi.GetData(client, token);

                // Proceed if response has content
                if (response.Content.Length > 2)
                {
                    // Deserialize the current records
                    List<EdFiStudentSpecialEducation> original = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);

                    foreach (var data in original)
                    {
                        // Update the endDate
                        data.endDate = spItem.EndDate;

                        // Serialize back to JSON
                        string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                        // Use the record's unique ID for the PUT call
                        var id = data.id;

                        // PUT the updated record back to ODS
                        var resp = edfiApi.PutData(
                            json,
                            new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id),
                            token);

                        Log.Info("updating enddate to student iep " + spItem.studentReference.studentUniqueId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values: " + ex.Message);
            }
        }
        /// <summary>
        /// Retrieves all Special Education program association records from the ODS for the specified plan,
        /// handling pagination, and returns a dictionary mapping studentUniqueId to UpdateEndDateStudent.
        /// </summary>
        /// <param name="specialEdPlan">A query string or identifier for the Special Education plan.</param>
        /// <param name="token">API authentication token.</param>
        /// <returns>
        /// A dictionary where the key is the student's unique ID and the value is the associated UpdateEndDateStudent object.
        /// </returns>
        private Dictionary<string, UpdateEndDateStudent> GetStudentSpecialEducation(string specialEdPlan, string token)
        {
            var lookup = new Dictionary<string, UpdateEndDateStudent>();

            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    int offset = 0, limit = 1000;
                    bool hasRecords = true;
                    while (hasRecords)
                    {
                        // Construct the client URL, using offset for pagination after the first batch
                        var client = offset == 0
                            ? new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + specialEdPlan)
                            : new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + specialEdPlan + "&offset=" + offset);

                        // Request the current batch from the ODS API
                        var response = edfiApi.GetData(client, token);
                        offset += limit;

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            Log.Error($"Unable to retrieve spEd list from {client.BaseUrl}");
                        }
                        else
                        {
                            // Deserialize the batch and add new students to the dictionary
                            var data = JsonConvert.DeserializeObject<List<UpdateEndDateStudent>>(response.Content);
                            foreach (var item in data)
                            {
                                if (!lookup.ContainsKey(item.studentReference.studentUniqueId))
                                    lookup.Add(item.studentReference.studentUniqueId, item);
                            }
                        }

                        // If the response is empty (e.g., "[]"), stop fetching
                        if (response.Content.Length <= 2)
                        {
                            hasRecords = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            return lookup;
        }


        /// <summary>
        /// Extracts IEP XML files from a deployment ZIP, cleans the directory, parses each XML, 
        /// and processes the Special Education program association data for each IEP node.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="prseXMl">Utility class for parsing XML documents.</param>
        public void UpdateIEPSpecialEducationProgramAssociationDataAsync(string token, ParseXmls prseXMl)
        {
            try
            {
                //-- TODO S3 Bucket                
                //S3Helper s3Helper = new S3Helper("your-access-key", "your-secret-key", "us-east-1");
                //await s3Helper.ListAndDownloadFilesAsync(Constants.bucket, Constants.prefix, Constants.directory);

                // Ensure the extracted XML directory exists and is clean
                string extractedPath = ConfigurationManager.AppSettings["XMLExtractedPath"];

                if (!Directory.Exists(extractedPath))
                    Directory.CreateDirectory(extractedPath);
                
                // Delete all existing files in the extracted directory
                foreach (System.IO.FileInfo file in new DirectoryInfo(extractedPath).GetFiles())
                    file.Delete();

                // Extract the deployment ZIP to the extracted directory
                string deploymentZipPath = ConfigurationManager.AppSettings["XMLDeploymentPath"] + ConfigurationManager.AppSettings["XMLZip"];
                ZipFile.ExtractToDirectory(deploymentZipPath, extractedPath);

                // Process each extracted XML file
                foreach (FileInfo file in new DirectoryInfo(extractedPath).GetFiles())
                {
                    // Clean up XML and correct any problematic characters
                    var fragments = File.ReadAllText(file.FullName)
                        .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "")
                        .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "")
                        .Replace("&", "&amp;");

                    // Convert string to XmlDocument
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(XDocument.Parse(fragments));

                    // Select all <iep> nodes under <root>
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");

                    // Process each IEP node
                    ProcessIEPXml(nodeList, token);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }


        /// <summary>
        /// Retrieves a list of student unique IDs from all IEP XML files in the extracted directory.
        /// </summary>
        /// <param name="prseXMl">Utility class for parsing XML documents.</param>
        /// <returns>A list of studentUniqueId strings from all <iep> nodes in the XML files.</returns>
        public List<string> GetStudentsInIEPXml(ParseXmls prseXMl)
        {
            List<string> students = new List<string>();
            try
            {
                // Iterate over each file in the extracted XML directory
                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    // Clean up the XML string and escape ampersands
                    var fragments = File.ReadAllText(file.FullName)
                        .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "")
                        .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "")
                        .Replace("&", "&amp;");

                    // Parse to XDocument then to XmlDocument
                    var doc = XDocument.Parse(fragments);
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);

                    // Select all <iep> nodes under <root>
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/iep");
                    foreach (XmlNode node in nodeList)
                    {
                        // Parse the node to extract student data
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

        /// <summary>
        /// Processes the data from the parsed xml and updates IEP data to ODS studentSpecialEducationProgramAssociations endpoint.
        /// </summary>
        /// <returns></returns>
        private void ProcessIEPXml(XmlNodeList nodeList, string token)
        {

            foreach (XmlNode node in nodeList)
            {
                // Parsing PCG IEP XML to get IEP data 
                var spEducationService = GetSpecialEducationXml(node);
                // In case of Duplicate Services in Xml the api request is not successfully posted through the api
                var spEducation = CheckDuplicateServices(spEducationService);

                //Getting Import disabilty desc from sims file
                //if (spedSimsLookup.TryGetValue(spEducation.studentUniqueId, out SpedSimsTxt studentSped))
                //{
                //    spEducation.levelofNeed = Constants.GetLevelOfNeed(studentSped.levelNeedInfo);
                //    spEducation.disability = Constants.GetDisabilityDescriptor(studentSped.disabilityInfo);
                //}


                //Check required field exist in XML source 
                if (!string.IsNullOrEmpty(spEducation.programEducationOrganizationId) && !string.IsNullOrEmpty(spEducation.programName) && !string.IsNullOrEmpty(spEducation.programTypeDescriptorId) && !string.IsNullOrEmpty(spEducation.studentUniqueId))
                {
                    // Check if the Program already exists in the ODS if not first enter the Progam.
                    VerifyProgramData(token, spEducation.programEducationOrganizationId, spEducation.programName, spEducation.programTypeDescriptorId);


                    // Insert if SignatureDate, IepBeginDate,IepEndDate is not null
                    if (!string.IsNullOrEmpty(spEducation.beginDate) && !string.IsNullOrEmpty(spEducation.iepBeginDate) && !string.IsNullOrEmpty(spEducation.iepEndDate))
                        InsertIEPStudentSpecialEducation(token, spEducation);

                    if (!string.IsNullOrEmpty(spEducation.iepExitDate))
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
                if (!String.IsNullOrEmpty(node.SelectSingleNode("iepUniqueId").InnerText))
                    spEducation.iepUniqueId = node.SelectSingleNode("iepUniqueId").InnerText.ToString() ?? null;
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
                    spEducation.programTypeDescriptorId = ProgramNode.SelectSingleNode("type").InnerText.ToString() ?? null;
                    spEducation.programName = ProgramNode.SelectSingleNode("name").InnerText.ToString() ?? null;
                    if (String.IsNullOrEmpty(spEducation.programName)) spEducation.programName = Constants.ProgramName;
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
                if (node.SelectSingleNode("schoolHoursPerWeek") != null) if (node.SelectSingleNode("schoolHoursPerWeek").InnerText.Trim().Length > 0)
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
                        SpecialEducationProgramServiceDescriptor = nvNode.SelectSingleNode("serviceDescriptor").InnerText.ToString() ?? null,
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
        /// Checks and consolidates duplicate related services in a student's Special Education program association,
        /// based on Service Descriptor and Service Location. Sums the service duration for duplicates and updates the
        /// corresponding properties before adding a single consolidated record back.
        /// </summary>
        /// <param name="spEducationServices">The student's Special Education program association object to process.</param>
        /// <returns>The same object, but with duplicate services merged and service duration updated accordingly.</returns>
        private static StudentSpecialEducationProgramAssociation CheckDuplicateServices(StudentSpecialEducationProgramAssociation spEducationServices)
        {
            // Group related services by ServiceDescriptor and ServiceLocation, find duplicates
            var dupService = spEducationServices.relatedServices
                .GroupBy(x => new { x.SpecialEducationProgramServiceDescriptor, x._ext.myBPS.serviceLocation })
                .Where(g => g.Count() > 1)
                .SelectMany(y => y)
                .ToList();

            // Get a single representative service for each duplicate group
            var dupServiceDis = spEducationServices.relatedServices
                .GroupBy(x => new { x.SpecialEducationProgramServiceDescriptor, x._ext.myBPS.serviceLocation })
                .Where(g => g.Count() > 1)
                .Select(y => y.First())
                .ToList();

            // For each duplicate combination, remove all and re-add a consolidated one
            if (dupServiceDis != null && dupServiceDis.Any())
            {
                foreach (var item in dupServiceDis)
                {
                    // Remove all matching duplicates from the list
                    spEducationServices.relatedServices.RemoveAll(r =>
                        r.SpecialEducationProgramServiceDescriptor == item.SpecialEducationProgramServiceDescriptor &&
                        r._ext.myBPS.serviceLocation == item._ext.myBPS.serviceLocation);

                    // Find all original duplicates for this descriptor-location
                    var dupSingleService = dupService
                        .Where(x => x.SpecialEducationProgramServiceDescriptor == item.SpecialEducationProgramServiceDescriptor &&
                                    x._ext.myBPS.serviceLocation == item._ext.myBPS.serviceLocation)
                        .ToList();

                    // Compute total minutes and update the item
                    item._ext.myBPS.serviceDuration = GetTotalMinutes(dupSingleService).ToString();
                    item._ext.myBPS.serviceDurationRecurrenceDescriptor = "day";
                    item._ext.myBPS.serviceDurationFrequency = "1";

                    // Add the consolidated service back
                    spEducationServices.relatedServices.Add(item);

                    Log.Info("Duplicate ServiceDescriptor exist for student: " +
                        spEducationServices.studentUniqueId + " Services: " +
                        item.SpecialEducationProgramServiceDescriptor);
                }
            }

            return spEducationServices;
        }


        /// <summary>
        /// Verifies the existence of a specific program in the Ed-Fi ODS. If the program does not exist,
        /// it creates a new program entry with the provided details.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="programEdOrgId">The education organization ID associated with the program.</param>
        /// <param name="programName">The name of the program.</param>
        /// <param name="programType">The type of the program.</param>
        public static void VerifyProgramData(string token, string programEdOrgId, string programName, string programType)
        {
            IRestResponse response = null;
            try
            {
                // Build the REST client with the appropriate API URL and parameters
                var client = new RestClient(
                    ConfigurationManager.AppSettings["ApiUrl"] +
                    Constants.API_Program +
                    Constants.educationOrganizationId + programEdOrgId +
                    Constants.programName + programName +
                    Constants.programType + programType);

                // Execute GET to check if the program exists
                response = edfiApi.GetData(client, token);

                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    // If no program found (empty response), create a new one
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
                            ProgramName = programName
                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = edfiApi.PostData(json,
                            new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program),
                            token);

                        Log.Info("Verify if the program data exists in EdFi Program for programTypeId: " + programType);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error getting the program data: " + ex);
            }
        }


        /// <summary>
        /// Inserts or updates a StudentSpecialEducationProgramAssociation record in the ODS based on the IEP file
        /// and existing memory cache data. Handles updates, deletions, and insertions as needed to synchronize data.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="spEducation">The student's Special Education program association data from the IEP.</param>
        private void InsertIEPStudentSpecialEducation(string token, StudentSpecialEducationProgramAssociation spEducation)
        {
            try
            {
                IRestResponse response = null;
                // Prepare the record to be sent to ODS
                var rootObject = GetSpecialEducation(token, spEducation);
                var httpClient = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation);
                // Memory cache lookup for existing StudentSpecialEducationProgramAssociation records
                var stuSpeEdIEPData = GetStudentSpecialEducation_IEP(token);

                // Key: (studentUniqueId, iepUniqueId)
                var studentSpedkey = new Tuple<string, string>(spEducation.studentUniqueId, spEducation.iepUniqueId);
                if (stuSpeEdIEPData.TryGetValue(studentSpedkey, out EdFiStudentSpecialEducation studentSped))
                {
                    string stuId = studentSped.studentReference.studentUniqueId;
                    bool flag = false;

                    // Compare SourceSystemId in ODS and file
                    string sysSrcId_ODS = studentSped._ext.myBPS.sourceSystemId;
                    string sysSrcId_file = spEducation.iepUniqueId;
                    if (sysSrcId_ODS != null && sysSrcId_file != null)
                        if (sysSrcId_ODS.Equals(sysSrcId_file)) flag = true;

                    if (studentSped.id != null)
                    {
                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        if (!string.IsNullOrEmpty(spEducation.beginDate))
                        {
                            if (stuId != null)
                            {
                                // Set default for programEducationOrganizationId if missing
                                if (spEducation.programEducationOrganizationId == "" || spEducation.programEducationOrganizationId.Equals(null))
                                    spEducation.programEducationOrganizationId = Constants.educationOrganizationIdValue;

                                // If primary fields match and source IDs are the same
                                if (stuId == spEducation.studentUniqueId && flag == true)
                                {
                                    // If any primary field is different, delete and insert new record
                                    if (!studentSped.beginDate.Equals(spEducation.beginDate) ||
                                        !studentSped.programReference.educationOrganizationId.Equals(spEducation.programEducationOrganizationId.TrimStart('0')) ||
                                        !studentSped.programReference.ProgramName.Equals(spEducation.programName))
                                    {
                                        // Delete the old record
                                        response = edfiApi.DeleteData(
                                            new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + studentSped.id),
                                            token);

                                        // Insert the new record
                                        if (IsSuccessStatusCode((int)response.StatusCode))
                                            response = edfiApi.PostData(json, httpClient, token);

                                        Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                                    }
                                }
                                // If sourceSystemId is different, it's a new insert
                                else if (!sysSrcId_ODS.Equals(sysSrcId_file))
                                {
                                    response = edfiApi.PostData(json, httpClient, token);
                                    Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                                    if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                                    {
                                        LogError(studentSped, response);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // No existing record found; insert new
                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    response = edfiApi.PostData(json, httpClient, token);
                    Log.Info("StudentSpecialEducation POST for studentUniqueId:" + spEducation.studentUniqueId);
                    if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                    {
                        //LogError(item, response);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
        }


        /// <summary>
        /// Updates the IEP exit date for a student's Special Education program association in the ODS,
        /// except for records with the 504 Plan program name.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="spEducation">The student's Special Education program association data with the new exit date.</param>
        private static void UpdateIEPStudentSpecialEducation(string token, StudentSpecialEducationProgramAssociation spEducation)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(
                    ConfigurationManager.AppSettings["ApiUrl"] +
                    Constants.StudentSpecialEducation +
                    "?studentUniqueId=" + spEducation.studentUniqueId +
                    Constants.programType + spEducation.programTypeDescriptorId);

                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                        foreach (var item in data)
                        {
                            // Skip records for 504 Plan
                            if (!item.programReference.ProgramName.Equals(Constants.programName504PlanValue))
                            {
                                var id = item.id;
                                if (id != null)
                                {
                                    // Update iepExitDate if provided
                                    item._ext.myBPS.iepExitDate = spEducation.iepExitDate;
                                    string json = JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented);
                                    if (item._ext != null)
                                    {
                                        if (!string.IsNullOrEmpty(item._ext.myBPS.iepExitDate))
                                        {
                                            response = edfiApi.PutData(
                                                json,
                                                new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id),
                                                token);
                                        }
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

        /// <summary>
        /// Calculates the total service duration in minutes across a list of duplicate related services,
        /// accounting for unit types, frequency, and recurrence descriptors.
        /// </summary>
        /// <param name="dupService">List of Service objects (duplicates with same descriptor/location).</param>
        /// <returns>Total minutes for all services in the list.</returns>
        private static int GetTotalMinutes(List<Service> dupService)
        {
            var TotalMinutes = 0;

            // Dictionary mapping unit types to their minute value
            var unitTypeList = new Dictionary<string, int>
        {
            { "minute(s)", 1 },
            { "hour(s)", 60 }
        };

            // Dictionary mapping recurrence types to their value (days)
            var recurrenceTypeList = new Dictionary<string, int>
        {
            { "5-day cycle", 5 },
            { "6-day cycle", 6 },
            { "7-day cycle", 7 },
            { "week", 5 },
            { "day", 1 },
            { "month", 20 },
            { "school year", 240 }
        };

            try
            {
                // Add up the service minutes for each service in the list
                foreach (var item in dupService)
                {
                    var serviceDurationUnit = item._ext.myBPS.serviceDurationUnitDescriptor ?? string.Empty;
                    var unitIndex = serviceDurationUnit.LastIndexOf("#");
                    if (unitIndex >= 0)
                    {
                        serviceDurationUnit = serviceDurationUnit.Substring(unitIndex + 1);
                    }

                    int duration = int.Parse(item._ext.myBPS.serviceDuration);
                    int unitMultiplier = unitTypeList.ContainsKey(serviceDurationUnit) ? unitTypeList[serviceDurationUnit] : 1;
                    int frequency = int.TryParse(item._ext.myBPS.serviceDurationFrequency, out int freq) ? (freq == 0 ? 1 : freq) : 1;
                    int recurrence = recurrenceTypeList.ContainsKey(item._ext.myBPS.serviceDurationRecurrenceDescriptor)
                        ? recurrenceTypeList[item._ext.myBPS.serviceDurationRecurrenceDescriptor]
                        : 1;

                    int serviceMinutes = (duration * unitMultiplier * frequency) / recurrence;

                    TotalMinutes += serviceMinutes;
                }

                return TotalMinutes;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return TotalMinutes;
            }
        }


        /// <summary>
        /// Constructs an EdFiStudentSpecialEducation object from a StudentSpecialEducationProgramAssociation,
        /// mapping all relevant fields and building references and related services appropriately.
        /// </summary>
        /// <param name="token">API authentication token, used for posting service descriptors if needed.</param>
        /// <param name="spList">Source association data from the IEP/XML file.</param>
        /// <returns>Fully populated EdFiStudentSpecialEducation object.</returns>
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

                // Handle disability if present
                if (spList.disability != null)
                {
                    var disability = new Disabilities
                    {
                        disabilityDescriptor = spList.disability,
                        orderOfDisability = 1
                    };
                    specialEducation.disabilities.Add(disability);
                }

                // Build related services if any
                if (spList.relatedServices != null)
                {
                    foreach (var serviceItem in spList.relatedServices)
                    {
                        var serLocation = Constants.GetServiceLocation(serviceItem._ext.myBPS.serviceLocation);
                        var array = new[] { serviceItem.SpecialEducationProgramServiceDescriptor, serLocation };
                        var ServiceDescLocation = string.Join(" - ", array.Where(s => !string.IsNullOrEmpty(s)));

                        // Ensure service descriptor exists in the system (could be a POST to a lookup table)
                        PostServiceDescriptor(ServiceDescLocation, token);

                        var relatedService = new Service
                        {
                            PrimaryIndicator = false, // default is false
                            SpecialEducationProgramServiceDescriptor = "uri://mybps.org/SpecialEducationProgramServiceDescriptor#" +
                                $"{string.Concat(ServiceDescLocation.ToString().Trim().Take(50))}",
                            ServiceBeginDate = serviceItem.ServiceBeginDate,
                            ServiceEndDate = serviceItem.ServiceEndDate,
                            _ext = new EdFiExtension()
                            {
                                myBPS = new Extension
                                {
                                    serviceDuration = serviceItem._ext.myBPS.serviceDuration,
                                    serviceDurationFrequency = serviceItem._ext.myBPS.serviceDurationFrequency,
                                    serviceDurationRecurrenceDescriptor = "uri://mybps.org/ServiceDurationRecurrenceDescriptor#" +
                                        Constants.GetSDRecurrenceDesc(serviceItem._ext.myBPS.serviceDurationRecurrenceDescriptor),
                                    serviceDurationUnitDescriptor = "uri://mybps.org/ServiceDurationUnitDescriptor#" +
                                        Constants.GetSDUnitDesc(serviceItem._ext.myBPS.serviceDurationUnitDescriptor),
                                    serviceLocation = serviceItem._ext.myBPS.serviceLocation,
                                    serviceClass = serviceItem._ext.myBPS.serviceClass
                                }
                            }
                        };

                        specialEducation.specialEducationProgramServices.Add(relatedService);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values: " + ex.Message);
            }
            return specialEducation;
        }
        
        
        /// <summary>
        /// Retrieves and caches a dictionary mapping (studentUniqueId, sourceSystemId) to EdFiStudentSpecialEducation objects
        /// for IEP special education program associations, excluding 504 Plan records.
        /// </summary>
        /// <param name="_accessToken">API authentication token.</param>
        /// <returns>
        /// Dictionary with Tuple(studentUniqueId, sourceSystemId) as key and EdFiStudentSpecialEducation as value.
        /// </returns>
        public Dictionary<Tuple<string, string>, EdFiStudentSpecialEducation> GetStudentSpecialEducation_IEP(string _accessToken)
        {
            try
            {
                // Return the cached value if already loaded
                if (_CACHE_SPED_IEP_LOOKUP != null)
                    return _CACHE_SPED_IEP_LOOKUP;

                var lookup = new Dictionary<Tuple<string, string>, EdFiStudentSpecialEducation>();

                if (!string.IsNullOrEmpty(_accessToken))
                {
                    int offset = 0, limit = 1000;
                    bool hasRecords = true;
                    while (hasRecords)
                    {
                        // Compose the API client for paginated retrieval
                        var client = offset == 0
                            ? new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + Constants.specialEdProgramTypeDescriptor)
                            : new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducationLimit + Constants.specialEdProgramTypeDescriptor + "&offset=" + offset);

                        var response = edfiApi.GetData(client, _accessToken);
                        offset += limit;

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            Log.Error($"Unable to retrieve spEd list from {client.BaseUrl}");
                        }
                        else
                        {
                            var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                            foreach (var item in data)
                            {
                                if (!item.programReference.ProgramName.Equals(Constants.programName504PlanValue))
                                {
                                    if (item._ext != null)
                                    {
                                        // Use (studentUniqueId, sourceSystemId) as the dictionary key
                                        lookup.Add(
                                            Tuple.Create(item.studentReference.studentUniqueId, item._ext.myBPS.sourceSystemId),
                                            item
                                        );
                                    }
                                }
                            }
                        }
                        if (response.Content.Length <= 2)
                        {
                            hasRecords = false;
                        }

                        // Cache the current lookup after each batch (optional, for failover)
                        _CACHE_SPED_IEP_LOOKUP = lookup;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong while getting data from ODS, check the XML values: " + ex.Message);
            }

            return _CACHE_SPED_IEP_LOOKUP;
        }

        
        /// <summary>
        /// Updates the iepExitDate for a student's 504 Plan Special Education program association in the ODS.
        /// Only records with ProgramName equal to the configured 504 Plan value are updated.
        /// </summary>
        /// <param name="token">API authentication token.</param>
        /// <param name="spEducation">SpecialEducation object containing StudentUniqueId and the updated IepExitDate.</param>
        public void UpdateAlertStudentSpecialEducation(string token, SpecialEducation spEducation)
        {
            try
            {
                IRestResponse response = null;
                // Build API client for fetching student special education records by studentUniqueId
                var client = new RestClient(
                    ConfigurationManager.AppSettings["ApiUrl"] +
                    Constants.StudentSpecialEducation +
                    "?studentUniqueId=" + spEducation.StudentUniqueId);

                response = edfiApi.GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        var data = JsonConvert.DeserializeObject<List<EdFiStudentSpecialEducation>>(response.Content);
                        foreach (var item in data)
                        {
                            // Only update the 504 Plan record
                            if (item.programReference.ProgramName.Equals(Constants.programName504PlanValue))
                            {
                                var id = item.id;
                                if (id != null)
                                {
                                    // Update iepExitDate and serialize
                                    item._ext.myBPS.iepExitDate = spEducation.IepExitDate;
                                    string json = JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented);

                                    if (item._ext != null && !string.IsNullOrEmpty(item._ext.myBPS.iepExitDate))
                                    {
                                        response = edfiApi.PutData(
                                            json,
                                            new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id),
                                            token);
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

        /// <summary>
        /// Constructs an EdFiStudentSpecialEducation object from a SpecialEducation alert object,
        /// mapping all relevant fields and building references and related services as needed.
        /// </summary>
        /// <param name="token">API authentication token, used for posting service descriptors if needed.</param>
        /// <param name="spList">Source SpecialEducation data from alert.</param>
        /// <returns>Fully populated EdFiStudentSpecialEducation object for alert context.</returns>
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

                // Add a transportation code service if service descriptor is present
                if (spList.ServiceDescriptor != null)
                {
                    PostServiceDescriptor(spList.ServiceDescriptor, token);
                    var transportationCodeService = new Service
                    {
                        PrimaryIndicator = false, // default is false
                        SpecialEducationProgramServiceDescriptor = "uri://mybps.org/SpecialEducationProgramServiceDescriptor#" +
                            $"{string.Concat(spList.ServiceDescriptor.ToString().Trim().Take(50))}",
                        ServiceBeginDate = spList.IepBeginDate,
                        ServiceEndDate = spList.IepEndDate,
                        _ext = new EdFiExtension()
                        {
                            myBPS = new Extension
                            {
                                serviceDuration = "0",
                                serviceDurationFrequency = "0",
                                serviceDurationRecurrenceDescriptor = "uri://mybps.org/ServiceDurationRecurrenceDescriptor#" +
                                    Constants.GetSDRecurrenceDesc(null),
                                serviceDurationUnitDescriptor = "uri://mybps.org/ServiceDurationUnitDescriptor#" +
                                    Constants.GetSDUnitDesc(null),
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
        /// Parses an XML node representing a SpecialEducation alert and returns a populated SpecialEducation object.
        /// </summary>
        /// <param name="node">The XML node containing the SpecialEducation data.</param>
        /// <returns>A SpecialEducation object with fields populated from the XML node, or null on error.</returns>
        private SpecialEducation GetAlertXml(XmlNode node)
        {
            try
            {
                string ServiceDesc = null;
                SpecialEducation studentEducationRefList = null;

                XmlNode ProgramNode = node.SelectSingleNode("programReference");
                XmlNode studentNode = node.SelectSingleNode("studentReference");
                XmlNode serviceDesc = node.SelectSingleNode("service");
                if (serviceDesc == null)
                    ServiceDesc = null;
                else
                    ServiceDesc = serviceDesc.SelectSingleNode("serviceDescriptor").InnerText;

                if (ProgramNode != null && studentNode != null)
                {
                    studentEducationRefList = new SpecialEducation
                    {
                        ProgramEducationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId")?.InnerText,
                        Type = ProgramNode.SelectSingleNode("type")?.InnerText,
                        Name = ProgramNode.SelectSingleNode("name")?.InnerText,
                        StudentUniqueId = studentNode.SelectSingleNode("studentUniqueId")?.InnerText,
                        ServiceDescriptor = ServiceDesc,
                        BeginDate = node.SelectSingleNode("iepSignatureDate")?.InnerText,
                        EndDate = node.SelectSingleNode("endDate")?.InnerText,
                        IdeaEligibility = node.SelectSingleNode("ideaEligiblity")?.InnerText == "true",
                        IepBeginDate = node.SelectSingleNode("iepBeginDate")?.InnerText,
                        IepEndDate = node.SelectSingleNode("iepEndDate")?.InnerText,
                        IepReviewDate = node.SelectSingleNode("iepReviewDate")?.InnerText,
                        IepParentResponse = node.SelectSingleNode("iepParentResponse")?.InnerText,
                        IepSignatureDate = node.SelectSingleNode("iepSignatureDate")?.InnerText,
                        Eligibility504 = node.SelectSingleNode("programEligibility")?.InnerText,
                        IepExitDate = node.SelectSingleNode("Section504ExitDate")?.InnerText
                    };
                }

                if (studentEducationRefList != null && string.IsNullOrEmpty(studentEducationRefList.EducationOrganizationId))
                    studentEducationRefList.EducationOrganizationId = Constants.educationOrganizationIdValue;

                return studentEducationRefList;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting Employment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;
            }
        }


        /// <summary>
        /// Extracts a list of student unique IDs from all XML files in the configured extracted path,
        /// looking for students in alert (504) XMLs.
        /// </summary>
        /// <param name="prseXMl">Parser helper for converting XDocument to XmlDocument.</param>
        /// <returns>List of student unique IDs present in 504 alert XMLs.</returns>
        public List<string> GetStudentsInAlertXml(ParseXmls prseXMl)
        {
            List<string> students = new List<string>();
            try
            {
                // Iterate through all files in the extracted XML directory
                foreach (FileInfo file in new DirectoryInfo(ConfigurationManager.AppSettings["XMLExtractedPath"]).GetFiles())
                {
                    // Read and clean the XML file (removing the XML declaration)
                    var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + "/504inXML.xml")
                                        .Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    var doc = XDocument.Parse(fragments);

                    // Convert to XmlDocument using provided parser helper
                    XmlDocument xmlDoc = prseXMl.ToXmlDocument(doc);

                    // Select all <student> nodes under <root>
                    XmlNodeList nodeList = xmlDoc.SelectNodes("//root/student");
                    foreach (XmlNode node in nodeList)
                    {
                        // Use GetAlertXml to parse the student info
                        var studentSpecialEducationList = GetAlertXml(node);
                        if (studentSpecialEducationList != null && !string.IsNullOrEmpty(studentSpecialEducationList.StudentUniqueId))
                        {
                            students.Add(studentSpecialEducationList.StudentUniqueId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return students;
        }


        /// <summary>
        /// Determines if the provided HTTP status code indicates a successful response (200-204 inclusive).
        /// </summary>
        /// <param name="statusCode">The HTTP status code to check.</param>
        /// <returns>True if the status code is between 200 and 204, inclusive; otherwise, false.</returns>
        private static bool IsSuccessStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode <= 204;
        }

        /// <summary>
        /// Posts a new ServiceDescriptor to the ODS API, using the provided item text as the basis for the descriptor fields.
        /// </summary>
        /// <param name="Item">The service descriptor string to use for code, description, etc.</param>
        /// <param name="token">API authentication token.</param>
        /// <returns>True if the post was attempted (does not guarantee API-side success).</returns>
        private static bool PostServiceDescriptor(string Item, string token)
        {
            bool isPosted = true;
            IRestResponse response = null;

            // Prepare REST client for Service Descriptor endpoint
            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_ProgramServiceDescriptor);

            // Construct the descriptor object, truncating fields to fit Ed-Fi limits
            var rootObject = new ServiceDescriptor
            {
                CodeValue = string.Concat(Item?.Trim().Take(50)),
                ShortDescription = string.Concat(Item?.Trim().Take(75)),
                Description = Item?.Trim(),
                EffectiveBeginDate = null,
                EffectiveEndDate = null,
                Namespace = "uri://mybps.org/SpecialEducationProgramServiceDescriptor",
                PriorDescriptorId = 0
            };

            // Serialize to JSON and POST
            string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
            response = edfiApi.PostData(json, client, token);

            // Check for unsuccessful status and log errors
            if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
            {
                Log.Error("Something went wrong while updating the Service Descriptor in ODS, check the XML values: " + response.Content.ToString());
                // Optionally, you may set isPosted to false here to reflect failure
            }

            return isPosted;
        }



        private static void LogError(EdFiStudentSpecialEducation item, IRestResponse response)
        {
            ErrorLog errorLog = new ErrorLog();
            errorLog.StudentLocalID = item.studentReference.studentUniqueId.Trim() ?? null;
            errorLog.EducationOrganizationId = item.programReference.educationOrganizationId ?? null;
            errorLog.Type = item.programReference.programTypeDescriptor ?? null;
            errorLog.Name = item.programReference.ProgramName ?? null;
            errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
            ErrorLogging(errorLog);
        }

        /// <summary>
        /// Logs an error entry to the log file specified in Constants.LOG_FILE. 
        /// If the file does not exist, it is created and a header row is added.
        /// </summary>
        /// <param name="errorLog">The error log object containing details to log.</param>
        private static void ErrorLogging(ErrorLog errorLog)
        {
            string strPath = Constants.LOG_FILE;
            bool isFirst = false;
            // Create the file if it doesn't exist and mark as first (for header)
            if (!File.Exists(strPath))
            {
                File.Create(strPath).Dispose();
                isFirst = true;
            }
            // Append the error entry (and header if first time)
            using (StreamWriter sw = File.AppendText(strPath))
            {
                if (isFirst)
                    sw.WriteLine("StudentId,EducationOrganizationId,ProgramTypeID,ProgramName,ErrorMessage");
                sw.WriteLine("{0},{1},{2},{3},{4}",
                    errorLog.StudentLocalID,
                    errorLog.EducationOrganizationId,
                    errorLog.Type,
                    errorLog.Name,
                    errorLog.ErrorMessage?.Replace(Environment.NewLine, " "));
            }
        }
    }
}