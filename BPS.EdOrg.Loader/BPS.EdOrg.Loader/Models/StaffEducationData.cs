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

        public List<StaffAddressData> Addresses { get; set; }
        public List<EdFiIdentificationCode> IdentificationCodes { get; set; }
        public List<StaffContactData> Telephones { get; set; }
        public List<StaffAddressData> InternationalAddresses { get; set; }
        public List<StaffElectronicMailsData> ElectronicMails { get; set; }
        public StaffEdFiExtension _ext { get; set; }

    }
    public class StaffEdFiExtension
    {
        public StaffEdFiExtension()
        { }
        public StaffExtension Staff { get; set; }
    }

    public class StaffExtension
    {
        public StaffExtension()
        { }
        public string unionCode { get; set; }
       
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
        public string department { get; set; }


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
        //public string HireDate { get; set; }
        //public string EmpDescriptor { get; set; }


    }
    public class EdFiProgram
    {
        public EdFiEducationReference EducationOrganizationReference { get; set; }
        public int? ProgramId { get; set; }
        public string ProgramTypeDescriptor { get; set; }
        public string SponsorType { get; set; }
        public string ProgramName { get; set; }
        public string ProgramEducationOrganizationId { get; set; }

    }

    

   
    public class EdFiEducationReference
    {
        public string educationOrganizationId { get; set; }
        public Link Link { get; set; }
    }

    public class ProgramReference
    {
        public string educationOrganizationId { get; set; }
        public string programTypeDescriptor { get; set; }
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



    public class SendEmail
    {
        public string FromAddr { get; set; }
        public ArrayList ToAddr { get; set; }
        public string EmailSubject { get; set; }
        public string EmailContent { get; set; }
        public string BccToAdr { get; set; }
        public List<Attachment> AttachmentList { get; set; }
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
        public string department { get; set; }
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

    public class EducationServiceCenterData
    {
        public string Id { get; set; }
        public string NameOfInstitution { get; set; }
        public string ShortNameOfInstitution { get; set; }
        public List<ServiceCategoryDescriptor> Categories { get; set; }
        public List<ServiceAddresses> Addresses { get; set; }
        public List<ServiceIdentificationCode> IdentificationCodes { get; set; }      
        public string EducationServiceCenterId { get; set; }
        public string OperationalStatusDescriptor { get; set; }


    }

    public class ServiceAddresses
    {
        public string StreetNumberName { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string StateAbbreviationDescriptor { get; set; }
        public string AddressTypeDescriptor { get; set; }

    }
    public class ServiceIdentificationCode
    {
        public string IdentificationCode { get; set; }
        public string EducationOrganizationIdentificationSystemDescriptor { get; set; }

    }
    public class ServiceCategoryDescriptor
    {
        public string EducationOrganizationCategoryDescriptor { get; set; }

    }
    public class StaffContactData
    {
        public string Id { get; set; }
        public string telephoneNumber { get; set; }
        public string telephoneNumberTypeDescriptor { get; set; }
        public string ext { get; set; }
        public bool textMessageCapabilityIndicator { get; set; }
        public string orderOfPriority { get; set; }

    }

   


    public class EdPlanToAspenTxt
    {
        public string studentNumber { get; set; }
        public string placementSchool { get; set; }
        public string program { get; set; }
        public string iepBeginDate { get; set; }
        public string iepEndDate { get; set; }
        public string iepReviewDate { get; set; }
        public string spEdExitDate { get; set; }
        public string nextEvalDate { get; set; }
        public string parentResponse { get; set; }
        public string dateSigned { get; set; }
        public string agency { get; set; }
        public string costShare { get; set; }

    }

    
    public class StaffAddressData
    {
        public string Id { get; set; }        
        public string streetNumberName { get; set; }
        public string apartmentRoomSuiteNumbe { get; set; }
        public string buildingSiteNumber { get; set; }
        public string city { get; set; }
        public string postalCode { get; set; }
        public string addressTypeDescriptor { get; set; }
        public string stateAbbreviationDescriptor { get; set; }
        public string localeDescriptor { get; set; }      
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string nameOfCounty { get; set; }

    }

    public class StaffElectronicMailsData
    {
        public string Id { get; set; }
        public string electronicMailAddress { get; set; }
         
        public string electronicMailTypeDescriptor { get; set; }       
        public bool primaryEmailAddressIndicator { get; set; }
       

    }


   

    public class StaffData
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string birthDate { get; set; }
        public string unionCode { get; set; }

    }
    
    public class UpdateEndDateStaff
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

    public class SponsoredStaff
    {
        public string userName { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string department { get; set; }
        public string positionTitle { get; set; }

    }


}

