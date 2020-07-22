using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;


namespace BPS.EdOrg.Loader.Models
{

    public class StaffDescriptor
    {
        public string Id { get; set; }
        public string StaffUniqueId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastSurname { get; set; }
        public string BirthDate { get; set; }
        public List<EdFiIdentificationCode> IdentificationCodes { get; set; }
        public List<StaffContactData> Telephones { get; set; }
        public List<StaffElectronicMailsData> ElectronicMails { get; set; }

    }

    public class EdFiIdentificationCode
    {
        public string StaffIdentificationSystemDescriptor { get; set; }
        public string IdentificationCode { get; set; }
        public string AssigningOrganizationIdentificationCode { get; set; }
    }
    public class StaffEmploymentDescriptor
    {

        public string id { get; set; }
        public EdFiEducationReference educationOrganizationReference { get; set; }
        public EdFiStaffReference staffReference { get; set; }
        public string hireDate { get; set; }
        public string endDate { get; set; }
        public string employmentStatusDescriptor { get; set; }

    }

    public class EdfiEmploymentAssociationReference
    {
        public string educationOrganizationId { get; set; }
        public string staffUniqueId { get; set; }
        public string hireDate { get; set; }
        public string employmentStatusDescriptor { get; set; }
        public Link Link { get; set; }

    }

    public class StaffSchoolAssociation
    {
        public EdFiSchoolReference SchoolReference { get; set; }
        public EdFiSchoolYearTypeReference SchoolYearTypeReference { get; set; }
        public EdFiStaffReference StaffReference { get; set; }
        public string ProgramAssignmentDescriptor { get; set; }


    }


    public class StaffAssignmentDescriptor
    {

        public string id { get; set; }
        public EdFiEducationReference EducationOrganizationReference { get; set; }
        public EdFiStaffReference StaffReference { get; set; }
        public EdfiEmploymentAssociationReference EmploymentStaffEducationOrganizationEmploymentAssociationReference { get; set; }
        public string PositionTitle { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public string StaffClassificationDescriptor { get; set; }
        public string OrderOfAssignment { get; set; }
        public string HireDate { get; set; }
        public string EmpDescriptor { get; set; }


    }
    public class EdFiProgram
    {
        public EdFiEducationReference EducationOrganizationReference { get; set; }
        public int? ProgramId { get; set; }
        public string Type { get; set; }
        public string SponsorType { get; set; }
        public string Name { get; set; }
        public string ProgramEducationOrganizationId { get; set; }

    }
    public class SpecialEducationReference
    {
        public SpecialEducationReference()
        { }
        public string id { get; set; }
        public EdFiEducationReference educationOrganizationReference { get; set; }
        public ProgramReference programReference { get; set; }
        public StudentReference studentReference { get; set; }
        public string beginDate { get; set; }
        public string endDate { get; set; }
        public bool ideaEligibility { get; set; }
        public string iepBeginDate { get; set; }
        public string iepEndDate { get; set; }
        public string iepParentResponse { get; set; }
        public string iepSignatureDate { get; set; }
        public string Eligibility504 { get; set; }
        public string iepReviewDate { get; set; }
        public string lastEvaluationDate { get; set; }
        public bool medicallyFragile { get; set; }
        public bool multiplyDisabled { get; set; }
        public string reasonExitedDescriptor { get; set; }
        public double schoolHoursPerWeek { get; set; }
        public bool servedOutsideOfRegularSession { get; set; }
        public double specialEducationHoursPerWeek { get; set; }
        public string specialEducationSettingDescriptor { get; set; }
        public List<Service> Services { get; set; }
        
    }

    public class EdFiEducationReference
    {
        public string educationOrganizationId { get; set; }
        public Link Link { get; set; }
    }

    public class ProgramReference
    {
        public string EducationOrganizationId { get; set; }
        public string ProgramTypeDescriptor { get; set; }
        public string ProgramName { get; set; }
        public Link Link { get; set; }
    }
    public class EdFiStaffReference
    {
        public string staffUniqueId { get; set; }
        public Link Link { get; set; }
    }
    public class EdFiSchoolReference
    {
        public string schoolId { get; set; }
        public Link Link { get; set; }
    }
    public class EdFiSchoolYearTypeReference
    {
        public string SchoolYear { get; set; }
        public Link Link { get; set; }
    }


    public class StudentReference
    {
        public string studentUniqueId { get; set; }
        public Link Link { get; set; }
    }

    public class Link
    {
        public string Rel { get; set; }
        public string Href { get; set; }
    }

    public class SchoolDept
    {
        public string SchoolId { get; set; }
        public string DeptId { get; set; }
        public string OperationalStatus { get; set; }
    }

    public class ErrorLog
    {
        public string StaffUniqueId { get; set; }
        public string EndDate { get; set; }
        public string ErrorMessage { get; set; }
        public string PositionTitle { get; set; }

    }

    public class SendEmail
    {
        public string FromAddr { get; set; }
        public ArrayList ToAddr { get; set; }
        public string EmailSubject { get; set; }
        public string EmailContent { get; set; }
        public string BccToAdr { get; set; }
        public List<Attachment> AttachmentList { get; set; }
    }

    public class StaffAssignmentAssociationData
    {
        public string Id { get; set; }
        public string StaffUniqueIdValue { get; set; }
        public string EmploymentEducationOrganizationIdValue { get; set; }
        public string PositionCodeDescription { get; set; }
        public string EducationOrganizationIdValue { get; set; }
        public string EndDateValue { get; set; }
        public string BeginDateValue { get; set; }
        public string HireDateValue { get; set; }
        public string StaffClassification { get; set; }
        public string EmpDesc { get; set; }
        public string JobOrderAssignment { get; set; }
    }
    public class StaffContactData
    {
        public string Id { get; set; }
        public string TelephoneNumber { get; set; }
        public string TelephoneNumberTypeDescriptor { get; set; }
        public string Ext { get; set; }
        public bool TextMessageCapabilityIndicator { get; set; }
        public string OrderOfPriority { get; set; }

    }
    public class StaffElectronicMailsData
    {
        public string Id { get; set; }
        public string electronicMailAddress { get; set; }
        public string electronicMailType { get; set; }       
        public bool primaryEmailAddressIndicator { get; set; }
       

    }
    public class StaffEmploymentAssociationData
    {

        public StaffData staff { get; set; }
        public string staffUniqueIdValue { get; set; }
        public string positionCodeDescription { get; set; }
        public string educationOrganizationIdValue { get; set; }
        public string endDateValue { get; set; }
        public string beginDateValue { get; set; }
        public string hireDateValue { get; set; }
        public string status { get; set; }
        public string staffClassification { get; set; }
        public string empDesc { get; set; }
        public string jobOrderAssignment { get; set; }
    }

    public class StaffData
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string birthDate { get; set; }

    }
    public class UpdateEndDate
    {
        public string StaffUniqueId { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
    }
    public class StaffAssociationData
    {
        public string staffId { get; set; }
        public string deptId { get; set; }
        public string action { get; set; }
        public string endDate { get; set; }
        public string hireDate { get; set; }
        public string jobCode { get; set; }
        public string jobTitle { get; set; }
        public string actionDate { get; set; }
        public string entryDate { get; set; }
        public string unionCode { get; set; }
        public string empClass { get; set; }
        public string descCode { get; set; }
        public string empCode { get; set; }
        public string jobIndicator { get; set; }
        public string status { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string birthDate { get; set; }

        public string location { get; set; }

    }
    public class StaffAssociationReference
    {
        public string id { get; set; }
    }

    public class StaffReference
    {
        public string StaffUniqueId { get; set; }
        public string FirstName { get; set; }
        public string LastSurname { get; set; }
        public List<StaffContactData> telephones { get; set; }
    }
}

