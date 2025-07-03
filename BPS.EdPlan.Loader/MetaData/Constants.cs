using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdPlanLoaderCore.MetaData
{
    /// <summary>
    /// Static string constants for constructing query parameters and default values related to education organization and program information.
    /// </summary>
    class Constants
    {
        public static string educationOrganizationId = @"?educationOrganizationId=";
        public static string SpecEduEducationOrganizationId = @"&educationOrganizationId=";
        public static string educationServiceCenterId = @"?educationServiceCenterId=";
        public static string educationOrganizationIdValue = @"350000";
        public static string educationOrganizationIdValueCentralStaff = @"9035";
        public static string StaffClassificationDefaultValue = @"Other";
        public static string employmentStatusDescriptorValue = @"Tenured%20or%20permanent";
        public static string beginDate = @"&beginDate=";
        public static string SpecEduBeginDate = @"?beginDate=";
        public static string beginDateDefaultValue = @"2018-09-04";
        public static string programName = @"&programName=";
        public static string programType = @"&programTypeDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#");
        public static string SpecEduProgramName = @"&programName=";
        public static string SpecEduProgramType = @"&programType=";
        public static string studentUniqueId = @"?studentUniqueId=";
        public static string SpecEduStudentUniqueId = @"&studentUniqueId=";
        public static string programEducationOrganizationId = @"&programEducationOrganizationId=";
        public static string schoolId = @"&schoolId=";
        public static string schoolId1 = @"?schoolId=";
        public static string sponsoredPositionTitle = @"#### - Sponsored Staff";
        public static string program504PlanValue = Uri.EscapeDataString(@"504 Plan");
        public static string program504Plan = @"&programName=" + program504PlanValue;
        public static string programName504PlanValue = "504 Plan";
        public static string ProgramName = "Special Education";
        public static string specialEdProgramTypeDescriptor = @"&programTypeDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#" + ProgramName);
        public static string alertProgramTypeDescriptor = @"&programTypeDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#" + "Section 504 Placement");
        public static string SchoolYear = ConfigurationManager.AppSettings["SchoolYear"];

        public static string ProgramAssignmentDescriptor = @"?programAssignmentDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramAssignmentDescriptor#Regular Education");
        public static string EmploymentStatusDescriptor = @"&employmentStatusDescriptor=" + Uri.EscapeDataString("uri://mybps.org/EmploymentStatusDescriptor#");
        public static string EmploymentStatusDescriptorOther = @"&employmentStatusDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/EmploymentStatusDescriptor#");
        public static string EmploymentStatusDescriptorField = "uri://mybps.org/EmploymentStatusDescriptor#";

        public static string EmploymentStatusDescriptorFieldOther = "uri://ed-fi.org/EmploymentStatusDescriptor#";
        public static string ProgramAssignmentDescriptorField = "uri://ed-fi.org/ProgramAssignmentDescriptor#Regular Education";
        public static string OperationalStatusActive = "uri://ed-fi.org/OperationalStatusDescriptor#Active";
        public static string OperationalStatusInactive = "uri://ed-fi.org/OperationalStatusDescriptor#Inactive";
        public static string Active = "Active";
        public static string PrimaryJobOrderAssignment = "1";
        // PCG file  flags
        public static bool ShouldExecuteIEPLoad { get; set; } = bool.Parse(ConfigurationManager.AppSettings["ShouldExecuteIEPLoad"]);
        public static bool ShouldExecuteAlertLoad { get; set; } = bool.Parse(ConfigurationManager.AppSettings["ShouldExecuteAlertLoad"]);
        //S3 bucket and file names
        public static string bucket = "bucket";
        public static string prefix = "prefix";
        public static string directory = "directory";

        public static string LOG_FILE { get; set; } = ConfigurationManager.AppSettings["LogFileDrive"] + DateTime.Today.ToString("yyyyMMdd") + ".csv";
        public static string LOG_FILE_ATT { get; set; } = @"Log File";
        public static string EmailFromAddress = ConfigurationManager.AppSettings["EmailFromAddr"];
        public static string LOG_FILE_REC { get; set; } = ConfigurationManager.AppSettings["ReviewTeam"];
        public static string LOG_FILE_SUB { get; set; } = @"IEPDATAReview";
        public static string LOG_FILE_BODY { get; set; } = @"IEPDATAReview Log File";

        public static string SmtpServerHost = ConfigurationManager.AppSettings["SmtpServerHost"];
        public static string EducationServiceCenter { get; set; } = @"ed-fi/educationServiceCenters";
         public static string API_Program { get; set; } = @"ed-fi/programs";
        public static string API_ProgramServiceDescriptor { get; set; } = @"ed-fi/specialEducationProgramServiceDescriptors";
        public static string StudentSpecialEducation { get; set; } = @"ed-fi/studentSpecialEducationProgramAssociations";
        public static string StudentSpecialEducationLimit { get; set; } = @"ed-fi/studentSpecialEducationProgramAssociations?limit=1000";
        public static string StudentProgramAssociation { get; set; } = @"ed-fi/studentProgramAssociations";
        public static string API_ServiceDescriptor { get; set; } = @"ed-fi/serviceDescriptors";
        public static string API_StudentSchoolAssociation { get; set; } = @"ed-fi/studentSchoolAssociations";
        public static string SchoolUrl { get; set; } = @"ed-fi/schools";
        public static string API_SpecialEdServiceDescriptor { get; set; } = @"ed-fi/specialEducationSettingDescriptors";
        
        public static string DataSourceXml { get; set; } = @"In Xml";

        public static string OutofBoundValue { get; set; } = "-1";

        // Get and set the GetEmpStatusDescp on desc code
        public static string GetEmpStatusDescp(string descCode)
        {
            if (descCode.Equals("Other"))
                return EmploymentStatusDescriptorOther;
            else
                return EmploymentStatusDescriptor;
        }

        // Get the Data Source as Txt or Xml for IEP and set it as source
        public static string GetDataSource(string dataSource)
        {
            if (dataSource == null || dataSource == "")
                return "In Xml";
            else
                return "In Txt";
        }

        //Gets current DateTime
        public static string GetCurrentDate()
        {
            return DateTime.Now.ToString("MM/dd/yyyy");
        }

        //GetSchoolID based on EducationOrgID
        public static string GetSchooId(string schoolId)
        {
            if (schoolId.Length >= 6)
                schoolId = Constants.educationOrganizationIdValueCentralStaff;

            return schoolId;
        }

        // Get the Data Source from ODS and verify if tit exists in Both Xml and Txt
        public static string SetDataSource(string dataSourceOp, string dataSourceIn)
        {
            if (dataSourceOp == "" || dataSourceOp == null)
                return dataSourceIn;
            if (dataSourceOp.Equals(dataSourceIn))
                return dataSourceIn;
            else
                return "In Both";

        }

        // Get and set the EmpDescField on desc code
        public static string GetEmpStatusDescpField(string descCode)
        {
            if (descCode.Equals("Other"))
                return EmploymentStatusDescriptorFieldOther;
            else
                return EmploymentStatusDescriptorField;



        }

        /// <summary>
        /// Returns "1" if the input string is "Y", otherwise returns "2".
        /// </summary>
        /// <param name="num">The input string to evaluate.</param>
        /// <returns>"1" if input is "Y"; otherwise, "2".</returns> 
        public static string GetPreferredNumber(string num)
        {
            if (num.Equals("Y"))
                return "1";
            else
                return "2";
        }

        /// <summary>
        /// Returns the provided service delivery recurrence description if it's not null or empty;
        /// otherwise, returns the default value "day".
        /// </summary>
        /// <param name="desc">The service delivery recurrence description.</param>
        /// <returns>
        /// The original description if not null or empty; otherwise, "day".
        /// </returns>
        public static string GetSDRecurrenceDesc(string desc)
        {
            if (!string.IsNullOrEmpty(desc))
                return desc;
            else
                return "day";
        }

        /// <summary>
        /// Returns the provided service delivery unit description if it's not null or empty; 
        /// otherwise, returns the default value "Minute(s)".
        /// </summary>
        /// <param name="desc">The service delivery unit description.</param>
        /// <returns>
        /// The original description if not null or empty; otherwise, "Minute(s)".
        /// </returns>
        public static string GetSDUnitDesc(string desc)
        {
            if (!string.IsNullOrEmpty(desc))
                return desc;
            else
                return "Minute(s)";
        }
       
        /// <summary>
        /// Gets the sei program by seicode.
        /// </summary>
        /// <param name="transCode"></param>
        /// <returns></returns>
        public static string GetTransportationEligibility(string transCode)
        {
            switch (transCode?.Trim())
            {

                case "Transportation - Door to Door":
                    return @"Door to Door";

                case "Transportation - Corner to Corner - Accommodated Corner":
                    return @"Accommodated";

                case "Transportation - Corner to Corner - Existing":
                    return @"Corner";

                case "Transportation - MBTA":
                    return @"T-Pass";

                default:
                    return @"Not Eligible";
            }
        }


        /// <summary>
        /// Converts a string indicator to a boolean value.
        /// Returns true if the input is "Y" or "True" (case-sensitive), otherwise false.
        /// </summary>
        /// <param name="strIndicator">The string indicator to convert.</param>
        /// <returns>
        /// True if the input is "Y" or "True"; otherwise, false.
        /// </returns>
        public static Boolean GetBoolIndicator(string strIndicator)
        {
            bool indicator = false;
            if (strIndicator == "Y" || strIndicator.Equals("Y") || strIndicator == "True" || strIndicator.Equals("True"))
            {
                indicator = true;
            }

            return indicator;

        }

        /// <summary>
        /// Determines the value of the primary indicator based on the input string.
        /// Returns false if the input is "Y" or "True" (case-sensitive), true otherwise.
        /// </summary>
        /// <param name="strIndicator">The indicator string to evaluate.</param>
        /// <returns>
        /// False if input is "Y" or "True", true otherwise.
        /// </returns>
        public static Boolean GetPrimaryIndicator(string strIndicator)
        {
            bool indicator = true;
            if (strIndicator == "Y" || strIndicator.Equals("Y") || strIndicator == "True" || strIndicator.Equals("True"))
            {
                indicator = false;
            }

            return indicator;

        }

        // <summary>
        /// Returns the standardized address descriptor. 
        /// Converts "MAIL" or "mail" to "Mailing", otherwise returns the input as is.
        /// </summary>
        /// <param name="desc">The address descriptor string to standardize.</param>
        /// <returns>
        /// "Mailing" if the input is "MAIL" or "mail"; otherwise, returns the input string unchanged.
        /// </returns>
        public static string GetAddressDescriptor(string desc)
        {
            if (desc == "MAIL" || desc == "mail")
            {

                desc = "Mailing";

            }
            return desc;
        }

        /// <summary>
        /// Maps a descriptive service location string to a standardized short code.
        /// Returns "In GE" if the input is "In general education classroom", 
        /// "Out GE" for any other non-empty input, or null if the input is null or empty.
        /// </summary>
        /// <param name="loc">The service location description.</param>
        /// <returns>
        /// "In GE" if the input is "In general education classroom",
        /// "Out GE" for any other non-empty input,
        /// null if the input is null or empty.
        /// </returns>
        public static string GetServiceLocation(string loc)
        {
            if (!string.IsNullOrEmpty(loc))
            {
                if (loc.Equals("In general education classroom"))
                    return "In GE";
                else return "Out GE";
            }
            return null;
        }

        /// <summary>
        /// Gets the LRE Setting code DesciptorId .
        /// </summary>
        /// <param name="descSetting"></param>
        /// <returns></returns>
        public static string GetSpecialEducationSetting(int? descSetting)
        {
            switch (descSetting)
            {
                case 30:
                case 32:
                case 20:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Inside reg class between 40-79% of the day";

                case 31:
                case 34:
                case 10:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Inside regular class 80% or more of the day";
                case 36:
                case 40:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Inside regular class less than 40% of the day";
                case 38:
                case 41:
                    return @"uri://mybps.org/SpecialEducationSettingDescriptor#Public Separate School";
                case 42:
                case 50:
                    return @"uri://mybps.org/SpecialEducationSettingDescriptor#Private Separate School";
                case 44:
                case 60:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Residential Facility";
                case 45:
                case 90:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Correctional Facilities";
                case 46:
                case 70:
                    return @"uri://ed-fi.org/SpecialEducationSettingDescriptor#Homebound/Hospital";
                default:
                    return null;
            }


        }
        /// <summary>
        /// Gets the LevelOfNeed DesciptorId .
        /// </summary>
        /// <param name="descSetting"></param>
        /// <returns></returns>
        public static string GetLevelOfNeed(string lodId)
        {
            switch (lodId)
            {
                case "04":
                    return @"uri://mybps.org/LevelOfNeedDescriptor#04";
                case "03":
                    return @"uri://mybps.org/LevelOfNeedDescriptor#03";
                case "02":
                    return @"uri://mybps.org/LevelOfNeedDescriptor#02";
                case "01":
                    return @"uri://mybps.org/LevelOfNeedDescriptor#01";
                default:
                    return null;
            }


        }

        /// <summary>
        /// Gets the Disability DesciptorId .
        /// </summary>
        /// <param name="descSetting"></param>
        /// <returns></returns>
        public static string GetDisabilityDescriptor(string disabilityId)
        {
            switch (disabilityId)
            {
                case "01":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Intellectual Disability";
                case "02":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Sensory impairment";
                case "03":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Speech or Language Impairment";
                case "04":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Visual Impairment, including Blindness";
                case "05":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Serious Emotional Disability";
                case "06":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Physical Disability";
                case "07":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Other Health Impairment";
                case "08":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Specific Learning Disability";
                case "09":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Deaf-Blindness";
                case "10":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Multiple Disabilities";
                case "11":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Autism Spectrum Disorders";
                case "12":
                    return @"uri://ed-fi.org/DisabilityDescriptor#Mental impairment";
                case "13":
                    return @"uri://mybps.org/DisabilityDescriptor#Developmental Delay (ages 3–9 only)";
                default:
                    return null;
            }


        }
    }
}
