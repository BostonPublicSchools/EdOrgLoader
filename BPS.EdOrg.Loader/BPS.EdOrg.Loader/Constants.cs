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
        public static string educationOrganizationId = @"?educationOrganizationId=";
        public static string educationOrganizationIdValue = @"350000";
        public static string employmentStatusDescriptor = @"&employmentStatusDescriptor=";
        public static string employmentStatusDescriptorValue = @"Tenured%20or%20permanent";
        public static string staffClassificationDescriptorId = @"&StaffClassificationDescriptorId=";
        public static string hireDate = @"&hireDate=";
        public static string beginDate = @"&beginDate=";
        public static string beginDateDefaultValue = @"2017-07-01";
        public static string staffUniqueId = @"&staffUniqueId=";
        public static string staffUniqueId1 = @"?staffUniqueId="; 
        public static string programName = @"&name=";
        public static string programType = @"&type=";
        public static string studentUniqueId = @"?studentUniqueId=";
        public static string programEducationOrganizationId = @"&programEducationOrganizationId=";


        public static string LOG_FILE { get; set; } = ConfigurationManager.AppSettings["LogFileDrive"] + DateTime.Today.ToString("yyyyMMdd") + ".csv";
        public static string LOG_FILE_ATT { get; set; } = @"Log File";
        public static string EmailFromAddress = ConfigurationManager.AppSettings["EmailFromAddr"];
        public static string LOG_FILE_REC { get; set; } = ConfigurationManager.AppSettings["ReviewTeam"];
        public static string LOG_FILE_SUB { get; set; } = @"EndDateDataReview";
        public static string LOG_FILE_BODY { get; set; } = @"EndDateDataReview Log File";

        public static string SmtpServerHost = ConfigurationManager.AppSettings["SmtpServerHost"];
        public static string StaffUrl { get; set; } = @"2019/staffs";
        public static string StaffEmploymentUrl { get; set; } = @"2019/staffEducationOrganizationEmploymentAssociations";
        public static string StaffAssignmentUrl { get; set; } = @"2019/staffEducationOrganizationAssignmentAssociations";
        public static string API_Program { get; set; } = @"2019/programs";
        public static string StudentSpecialEducation { get; set; } = @"2019/studentSpecialEducationProgramAssociations";

       public static string EmpClassCode(string empCode)
       {
            if (empCode.Equals("4") || empCode.Equals("P"))
                return "Tenured or permanent";
            if (empCode.Equals("Q") || empCode.Equals("2") || empCode.Equals("3"))
                return "Substitute/temporary";
            else
                return "Other";
        }


        public static string JobOrderAssignment(string jobcode)
        {
            if (jobcode.Equals("P"))
                return "1";
            if (jobcode.Equals("S"))
                return "2";
            else
                return "0";
        }
        public static string GetDeptMultipleLocation(string loc)
        {
            if (loc.Equals("BPS008"))
                return "101206";
            if (loc.Equals("BPS045"))
                return "101411";
            if (loc.Equals("BPS046"))
                return "101351";
            if (loc.Equals("BPS140"))
                return "101204";
            if (loc.Equals("BPS158")) 
                return "101307";
            if (loc.Equals("BPS161"))
                return "101256";
            else
                return null;
        }

        public static string StaffClassificationDescriptorCode(string jobCode, int deptID, string unionCode)
       {
            if (jobCode.Equals("S00022") || jobCode.Equals("S00023") || jobCode.Equals("S00170") ||
                jobCode.Equals("S00167") || jobCode.Equals("S00200") || jobCode.Equals("S00218") ||
                jobCode.Equals("S00340") || jobCode.Equals("S00445") || jobCode.Equals("S20324") || 
                jobCode.Equals("S01077") && (deptID >= 101200 && 101699 <= deptID))
                return "School Leader";

            if (jobCode.Equals("S00065") || jobCode.Equals("S00169") || jobCode.Equals("S00183") ||
                jobCode.Equals("S00257") || jobCode.Equals("S00281") || jobCode.Equals("S00354") ||
                jobCode.Equals("S00406") || jobCode.Equals("S00407") || jobCode.Equals("S00413") ||
                jobCode.Equals("S01070") || jobCode.Equals("S20113") || jobCode.Equals("S20201") ||
                jobCode.Equals("S20267") || jobCode.Equals("S20302") && (deptID >= 101200 && 101699 <= deptID))
                return "School Specialist";

            if (jobCode.Equals("S00116") || jobCode.Equals("S00118") || jobCode.Equals("S00220") ||
                jobCode.Equals("S00245") || jobCode.Equals("S00465") || jobCode.Equals("S01079") ||
                jobCode.Equals("S11100") || jobCode.Equals("S85026") && (unionCode.Equals("BAS") || unionCode.Equals("BPS")) &&
                (deptID >= 101200 && 101699 <= deptID))
                return "School Administrator";

            if (jobCode.Equals("S00116") || jobCode.Equals("S00118") || jobCode.Equals("S00220") ||
                jobCode.Equals("S00245") || jobCode.Equals("S00465") || jobCode.Equals("S01079") ||
                jobCode.Equals("S11100") || jobCode.Equals("S85026") &&
                (deptID >= 101000 && deptID <= 101199 || deptID >= 101000 && deptID <= 101199))
                return "LEA Administrator";

            if (jobCode.Equals("S00065") || jobCode.Equals("S00169") || jobCode.Equals("S00183") ||
                jobCode.Equals("S00257") || jobCode.Equals("S00281") || jobCode.Equals("S00354") ||
                jobCode.Equals("S00406") || jobCode.Equals("S00407") || jobCode.Equals("S00413") ||
                jobCode.Equals("S01070") || jobCode.Equals("S20113") || jobCode.Equals("S20201") ||
                jobCode.Equals("S20267") || jobCode.Equals("S20302") &&
                (deptID >= 101000 && deptID <= 101199 || deptID >= 101000 && deptID <= 101199))
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
        
    }
}
