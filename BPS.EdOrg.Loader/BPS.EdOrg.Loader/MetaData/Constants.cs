using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    class Constants
    {
        public static string JobFile = @"R0100156_JOB_W_ORIGHIRDT.txt";
        public static string JobEdPlanTxtFile = @"EdPlanToAspen.txt";
        public static string JobEdPlanXmlFile = @"R137!maboston!0-26-19-079949!4-5-22.xml";
        public static string educationOrganizationId = @"?educationOrganizationId=";
        public static string SpecEduEducationOrganizationId = @"&educationOrganizationId=";
        public static string educationServiceCenterId = @"?educationServiceCenterId=";
        public static string educationOrganizationIdValue = @"350000";
        public static string educationOrganizationIdValueCentralStaff = @"9035";        
        public static string employmentStatusDescriptorValue = @"Tenured%20or%20permanent";
        public static string staffClassificationDescriptorId = @"&StaffClassificationDescriptor="+StaffClassificationDescriptor;
        public static string orderofAssignment = "&orderofAssignment=";
        public static string hireDate = @"&hireDate=";
        public static string beginDate = @"&beginDate=";
        public static string SpecEduBeginDate = @"?beginDate=";
        public static string beginDateDefaultValue = @"2018-09-04";
        public static string staffUniqueId = @"&staffUniqueId=";
        public static string staffUniqueId1 = @"?staffUniqueId="; 
        public static string programName = @"&programName=";
        public static string programType = @"&programTypeDescriptor="+ Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#");
        public static string SpecEduProgramName = @"&programName=";
        public static string SpecEduProgramType = @"&programType=";
        public static string studentUniqueId = @"?studentUniqueId=";
        public static string SpecEduStudentUniqueId = @"&studentUniqueId=";
        public static string programEducationOrganizationId = @"&programEducationOrganizationId=";        
        public static string schoolId = @"&schoolId=";
        public static string schoolId1=  @"?schoolId=";
        public static string program504PlanValue = Uri.EscapeDataString(@"504 Plan");
        public static string program504Plan = @"&programName="+program504PlanValue;
        public static string programName504PlanValue = "504 Plan";
        public static string ProgramName = "Special Education";
        public static string specialEdProgramTypeDescriptor = @"&programTypeDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#"+ ProgramName);
        public static string alertProgramTypeDescriptor = @"&programTypeDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/ProgramTypeDescriptor#" + "Section 504 Placement");


        public static string StaffIdentificationSystemDescriptor = "uri://ed-fi.org/StaffIdentificationSystemDescriptor#State";
        public static string ProgramAssignmentDescriptor = @"?programAssignmentDescriptor =" + Uri.EscapeDataString("uri://ed-fi.org/ProgramAssignmentDescriptor#Regular Education ");
        public static string EmploymentStatusDescriptor = @"&employmentStatusDescriptor=" + Uri.EscapeDataString("uri://mybps.org/EmploymentStatusDescriptor#");
        public static string EmploymentStatusDescriptorOther = @"&employmentStatusDescriptor=" + Uri.EscapeDataString("uri://ed-fi.org/EmploymentStatusDescriptor#");
        public static string StaffClassificationDescriptor1 = @"& StaffClassificationDescriptor = " +Uri.EscapeDataString("uri://ed-fi.org/StaffClassificationDescriptor#");
        public static string StaffClassificationDescriptor = @"&uri://ed-fi.org/StaffClassificationDescriptor#";
        public static string EmploymentStatusDescriptorField = "uri://mybps.org/EmploymentStatusDescriptor#";

        public static string EmploymentStatusDescriptorFieldOther = "uri://ed-fi.org/EmploymentStatusDescriptor#";
        public static string StaffClassificationDescriptorField = "uri://ed-fi.org/StaffClassificationDescriptor#";
        public static string ProgramAssignmentDescriptorField = "uri://ed-fi.org/ProgramAssignmentDescriptor#Regular Education";
        public static string OperationalStatusActive = "uri://ed-fi.org/OperationalStatusDescriptor#Active";
        public static string OperationalStatusInactive = "uri://ed-fi.org/OperationalStatusDescriptor#Inactive";
        public static string Active = "Active";
        public static string LOG_FILE { get; set; } = ConfigurationManager.AppSettings["LogFileDrive"] + DateTime.Today.ToString("yyyyMMdd") + ".csv";
        public static string LOG_FILE_ATT { get; set; } = @"Log File";
        public static string EmailFromAddress = ConfigurationManager.AppSettings["EmailFromAddr"];
        public static string LOG_FILE_REC { get; set; } = ConfigurationManager.AppSettings["ReviewTeam"];
        public static string LOG_FILE_SUB { get; set; } = @"IEPDATAReview";
        public static string LOG_FILE_BODY { get; set; } = @"IEPDATAReview Log File";

        public static string SmtpServerHost = ConfigurationManager.AppSettings["SmtpServerHost"];
        public static string StaffUrl { get; set; } = @"ed-fi/staffs";
        public static string StaffEmploymentUrl { get; set; } = @"ed-fi/staffEducationOrganizationEmploymentAssociations";
        public static string EducationServiceCenter { get; set; } = @"ed-fi/educationServiceCenters";
        public static string StaffAssignmentUrl { get; set; } = @"ed-fi/staffEducationOrganizationAssignmentAssociations";
        public static string API_Program { get; set; } = @"ed-fi/programs";
        public static string API_ProgramServiceDescriptor { get; set; } = @"ed-fi/specialEducationProgramServiceDescriptors";
        public static string StudentSpecialEducation { get; set; } = @"ed-fi/studentSpecialEducationProgramAssociations";
        public static string StudentSpecialEducationLimit { get; set; } = @"ed-fi/studentSpecialEducationProgramAssociations?limit=100";
        public static string StudentProgramAssociation { get; set; } = @"ed-fi/studentProgramAssociations";
        public static string API_ServiceDescriptor { get; set; } = @"ed-fi/serviceDescriptors";
        public static string SchoolUrl { get; set; } = @"ed-fi/schools";
        public static string API_SpecialEdServiceDescriptor { get; set; } = @"ed -fi/specialEducationSettingDescriptors";
        public static string StaffAssociationUrl { get; set; } = @"ed-fi/staffSchoolAssociations";

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


        // Set the EmpClassCode based on the empCode 
        public static string EmpClassCode(string empCode)
        {
                if (empCode.Equals("4") || empCode.Equals("P"))
                    return "Permanent Academic";
                if (empCode.Equals("Q") || empCode.Equals("2") || empCode.Equals("3"))
                    return "Substitute";
                if (empCode.Equals("U"))
                    return "1st Year Provisional";
                if (empCode.Equals("V"))
                    return "2nd Year Provisional";
                if (empCode.Equals("W"))
                    return "3rd Year Provisional";
                if (empCode.Equals("X"))
                    return "4th Year Provisional";
                else
                    return "Other";
            
        }

        // Set the JobOrderAssignment based on the jobcode 
        public static string JobOrderAssignment(string jobcode)
        {
            if (jobcode.Equals("P"))
                return "1";
            if (jobcode.Equals("S"))
                return "2";
            else
                return "0";
        }

        // Set the TelephoneType based on the type 
        public static string GetTelephoneType(string type)
        {
            if (type.Equals("HOME") || type.Equals("Home"))
                return "Home";
            if (type.Equals("CELL") || type.Equals("Cell"))
                return "Mobile";
            if (type.Equals("BUSN") || type.Equals("Busn"))
                return "Work";
            if (type.Equals("FAX") || type.Equals("Fax"))
                return "Fax";
            if (type.Equals("PGR1") || type.Equals("PAGR"))
                return "Unlisted";
            else
                return "Other";
        }
        public static string GetPreferredNumber(string num)
        {
            if (num.Equals("Y"))
                 return "1";
            else
                return "2";
        }

        
        public static string GetSDRecurrenceDesc(string desc)
        {
            if (!string.IsNullOrEmpty(desc))
                return desc;
            else
                return "day";
        }
        public static string GetSDUnitDesc(string desc)
        {
            if (!string.IsNullOrEmpty(desc))
                return desc;
            else
                return "Minute(s)";
        }
        public static string StaffClassificationDescriptorCode(string jobCode, int deptID, string unionCode)
       {
            if (jobCode.Equals("S00022") || jobCode.Equals("S00023") || jobCode.Equals("S00170") ||
                jobCode.Equals("S00167") || jobCode.Equals("S00200") || jobCode.Equals("S00218") ||
                jobCode.Equals("S00340") || jobCode.Equals("S00445") || jobCode.Equals("S20324") || 
                jobCode.Equals("S01077") && deptID >= 101200 && deptID <= 101699)
                return "School Leader";

            if (jobCode.Equals("S00065") || jobCode.Equals("S00169") || jobCode.Equals("S00183") ||
                jobCode.Equals("S00257") || jobCode.Equals("S00281") || jobCode.Equals("S00354") ||
                jobCode.Equals("S00406") || jobCode.Equals("S00407") || jobCode.Equals("S00413") ||
                jobCode.Equals("S01070") || jobCode.Equals("S20113") || jobCode.Equals("S20201") ||
                jobCode.Equals("S20267") || jobCode.Equals("S20302") && deptID >= 101200 && deptID <= 101699 )
                return "School Specialist";

            if (jobCode.Equals("S00116") || jobCode.Equals("S00118") || jobCode.Equals("S00220") ||
                jobCode.Equals("S00245") || jobCode.Equals("S00465") || jobCode.Equals("S01079") ||
                jobCode.Equals("S11100") || jobCode.Equals("S85026") && (unionCode.Equals("BAS") || unionCode.Equals("BPS")) &&
                deptID >= 101200 && deptID <= 101699)
                return "School Administrator";

            if (jobCode.Equals("S00116") || jobCode.Equals("S00118") || jobCode.Equals("S00220") ||
                jobCode.Equals("S00245") || jobCode.Equals("S00465") || jobCode.Equals("S01079") ||
                jobCode.Equals("S11100") || jobCode.Equals("S85026") &&
                (deptID >= 101000 && deptID <= 101199 || deptID >= 101700 && deptID <= 101999))
                return "LEA Administrator";

            if (jobCode.Equals("S00065") || jobCode.Equals("S00169") || jobCode.Equals("S00183") ||
                jobCode.Equals("S00257") || jobCode.Equals("S00281") || jobCode.Equals("S00354") ||
                jobCode.Equals("S00406") || jobCode.Equals("S00407") || jobCode.Equals("S00413") ||
                jobCode.Equals("S01070") || jobCode.Equals("S20113") || jobCode.Equals("S20201") ||
                jobCode.Equals("S20267") || jobCode.Equals("S20302") &&
                (deptID >= 101000 && deptID <= 101199 || deptID >= 101700 && deptID <= 101999))
                return "LEA Specialist";

            var strings = new List<string> { "S20113", "S20100", "S20100", "S20315", "S20310", "S01080" };
            string x = jobCode;
            bool contains = !strings.Contains(x, StringComparer.OrdinalIgnoreCase) && unionCode.Equals("BT1");
            if (contains)
                return "Instructional Aide";

            if (jobCode.Equals("S01071") || jobCode.Equals("S01080") || jobCode.Equals("S20207") ||
                jobCode.Equals("S20301") || jobCode.Equals("S20304") || jobCode.Equals("S21000") && unionCode.Equals("BT3"))
                return "Instructional Coordinator";

            if (jobCode.Equals("S10120")|| jobCode.Equals("S10121") || jobCode.Equals("S10110"))
                return "Assistant Principal";

            if (jobCode.Equals("S00156") || jobCode.Equals("S00230") || jobCode.Equals("S00251")||
                jobCode.Equals("S00252") || jobCode.Equals("S01089") || jobCode.Equals("S00250") ||
                jobCode.Equals("S00254") || jobCode.Equals("S00237") || jobCode.Equals("S01075") ||
                jobCode.Equals("S01077") || jobCode.Equals("S01078") || jobCode.Equals("S01092"))
                return "Assistant Superintendent";

            if (jobCode.Equals("S20226") || jobCode.Equals("S20230") || jobCode.Equals("S20233") ||
                jobCode.Equals("S20213") || jobCode.Equals("S20252") || jobCode.Equals("S20304")) 
                return "Counselor";

            if (unionCode.Equals("HMP"))
                return "Principal";

            if (unionCode.Equals("BT2"))
                return "Substitute Teacher";

            if (unionCode.Equals("AGU"))
                return "Support Services Staff";   
                       
            if (jobCode.Equals("S20264") || jobCode.Equals("S20267"))
                     return "Librarians/Media Specialists";

            if (jobCode.Equals("S00023") || jobCode.Equals("S00191") || jobCode.Equals("S00204") ||
                jobCode.Equals("S00239") || jobCode.Equals("S00352") || jobCode.Equals("S01079") ||
                jobCode.Equals("S01091") || jobCode.Equals("S20275") || jobCode.Equals("S20321") ||
                jobCode.Equals("S30120") || jobCode.Equals("S30145") || jobCode.Equals("S30170") ||
                jobCode.Equals("S30327") || jobCode.Equals("S30329") || jobCode.Equals("S30350") || jobCode.Equals("S30365"))
                    return "Operational Support";
            
            if (jobCode.Equals("S00420"))
                    return "Superintendent";

            if (jobCode.Equals("S01080") || jobCode.Equals("S20315") || jobCode.Equals("S20310"))
                    return "Teacher";

            else
                    return "Other";
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

        public static Boolean GetBoolIndicator(string strIndicator)
        {
            bool indicator = false;
            if (strIndicator == "Y" || strIndicator.Equals("Y") || strIndicator == "True"|| strIndicator.Equals("True"))
            {
                indicator = true;
            }

            return indicator;

        }
        public static Boolean GetPrimaryIndicator(string strIndicator)
        {
            bool indicator = true;
            if (strIndicator == "Y" || strIndicator.Equals("Y") || strIndicator == "True" || strIndicator.Equals("True"))
            {
                indicator = false;
            }

            return indicator;

        }

        
        public static string GetAddressDescriptor(string desc)
        {
            if (desc == "MAIL" || desc == "mail")
            {
                
             desc =  "Mailing";
                
            }
            return desc;
        }


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
      
    }

}
