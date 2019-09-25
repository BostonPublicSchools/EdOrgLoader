using log4net;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    public class RestServiceManager
    {
        private readonly Configuration _configuration = null;
        private readonly string _accessToken = string.Empty;
        private readonly ILog _log;
        public RestServiceManager(Configuration configuration,string token, ILog logger)
        {
            _configuration = configuration;
            _accessToken = token;
            _log = logger;
        }

        public List<SchoolDept> GetSchoolList()
        {
            List<string> existingDeptIds = new List<string>();
            List<SchoolDept> schoolDepts = new List<SchoolDept>();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    var httpClient = new RestClient(_configuration.CrossWalkSchoolApiUrl);
                    int offset = 0, limit = 100;
                    bool hasRecords = true;
                    while (hasRecords)
                    {
                        var response = GetRestResponse(httpClient, offset, limit);
                        offset += limit;
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _log.Error($"Unable to retrieve school list from {httpClient.BaseUrl}");
                        }
                        else
                        {
                            List<SchoolResponse> schoolList = response.Data;
                            foreach (var school in schoolList)
                            {
                                string deptId = school.IdentificationCodes?
                                    .Where(x => string.Equals(x.EducationOrganizationIdentificationSystemDescriptor, "school", StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault()?.IdentificationCode;
                                if (deptId == null || deptId.Equals("N/A"))
                                    deptId = school.schoolId;
                                if (!string.IsNullOrEmpty(deptId) && !string.Equals(deptId, "N/A", StringComparison.OrdinalIgnoreCase))
                                {
                                    existingDeptIds.Add(deptId);
                                    schoolDepts.Add(new SchoolDept { schoolId = school.schoolId, DeptId = deptId });
                                }
                            }
                        }

                        if (response.Data.Count == 0)
                        {
                            hasRecords = false;
                        }
                    }
                   
                }
                else
                {
                    _log.Error($"Bearer token not provided for retrieving school list from API");
                }               
            }
            catch(Exception ex)
            {
                _log.Error($"Exception while retrieve school list : {ex.Message}");
            }
            return schoolDepts;
        }

        public List<SchoolDept> GetServiceCenterList()
        {
            List<SchoolDept> schoolDepts = new List<SchoolDept>();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    var httpClient = new RestClient(_configuration.CrossWalkServiceCenterApiUrl);
                    int offset = 0, limit = 100;
                    bool hasRecords = true;
                    while (hasRecords)
                    {
                        var response = GetRestResponse(httpClient, offset, limit);
                        offset += limit;
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _log.Error($"Unable to retrieve staff list from {httpClient.BaseUrl}");
                        }
                        else
                        {
                            List<SchoolResponse> serviceCenterList = response.Data;
                            foreach (var school in serviceCenterList)
                            {
                                string deptId = school.IdentificationCodes?
                                    .Where(x => string.Equals(x.EducationOrganizationIdentificationSystemDescriptor, "school", StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault()?.IdentificationCode;
                               
                                if (!string.IsNullOrEmpty(deptId) && !string.Equals(deptId, "N/A", StringComparison.OrdinalIgnoreCase))
                                {
                                    schoolDepts.Add(new SchoolDept { schoolId = school.schoolId, DeptId = deptId });
                                }
                            }
                        }

                        if (response.Data.Count == 0)
                        {
                            hasRecords = false;
                        }
                    }

                }
                else
                {
                    _log.Error($"Bearer token not provided for retrieving staff list from API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception while retrieve serviceCenter list : {ex.Message}");
            }
            return schoolDepts;
        }
        public List<string> GetStaffList()
        {
            List<string> existingStaffIds = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    var httpClient = new RestClient(_configuration.CrossWalkStaffApiUrl);
                    int offset = 0, limit = 100;
                    bool hasRecords = true;
                    while (hasRecords)
                    {
                        var response = GetRestResponseStaff(httpClient, offset, limit);
                        offset += limit;
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _log.Error($"Unable to retrieve staff list from {httpClient.BaseUrl}");
                        }
                        else
                        {
                            List<StaffResponse> staffList = response.Data;
                            foreach (var staff in staffList)
                            {
                                string staffId = staff.staffUniqueId[0].ToString();
                                    
                                if (!string.IsNullOrEmpty(staffId) && !string.Equals(staffId, "N/A", StringComparison.OrdinalIgnoreCase))
                                {
                                    existingStaffIds.Add(staffId);
                                }
                            }
                        }

                        if (response.Data.Count == 0)
                        {
                            hasRecords = false;
                        }
                    }

                }
                else
                {
                    _log.Error($"Bearer token not provided for retrieving staff list from API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception while retrieve staff list : {ex.Message}");
            }
            return existingStaffIds;
        }




        private IRestResponse<List<SchoolResponse>> GetRestResponse(RestClient httpClient, int offset , int limit)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("offset", offset);
            request.AddParameter("limit", limit);
            var response = httpClient.Execute<List<SchoolResponse>>(request);
            return response;
        }

        private IRestResponse<List<StaffResponse>> GetRestResponseStaff(RestClient httpClient, int offset, int limit)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("offset", offset);
            request.AddParameter("limit", limit);
            var response = httpClient.Execute<List<StaffResponse>>(request);
            return response;
        }

        
    }
}
