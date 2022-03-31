using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader.Models
{
        /// <summary>
        /// Service Descriptor
        /// </summary>
         public class ServiceDescriptor
         {
            public string CodeValue { get; set; }
            public string Description { get; set; }
            public string EffectiveBeginDate { get; set; }
            public string EffectiveEndDate { get; set; }
            public string Namespace { get; set; }
            public int PriorDescriptorId { get; set; }
            public string ShortDescription { get; set; }
            
         }

         /// <summary>
         /// Service
        /// </summary>
        public class Service
        {
            public string SpecialEducationProgramServiceDescriptor { get; set; }
            public bool PrimaryIndicator { get; set; }
            public string ServiceBeginDate { get; set; }
            public string ServiceEndDate { get; set; }
            public EdFiExtension _ext { get; set; }
            
        }
        public class EdFiExtension
        {
        public EdFiExtension()
        { }
        public Extension myBPS { get; set; }
        }
        public class Extension
        {
            public Extension()
            { }
            public string serviceDurationRecurrenceDescriptor { get; set; }
            public string serviceDurationUnitDescriptor { get; set; }
            public string serviceDuration { get; set; }
            public string serviceDurationFrequency { get; set; }
            public string serviceLocation { get; set; }
            public string serviceClass { get; set; }

    }


    public class StaffAlertDescriptor
        {

            public string id { get; set; }
            public EdFiEducationReference educationOrganizationReference { get; set; }
            public EdFiStaffReference staffReference { get; set; }
            public string hireDate { get; set; }
            public string endDate { get; set; }
            public string employmentStatusDescriptor { get; set; }

        }
        public class SpecialEducation
        {
        public SpecialEducation()
        { }
        public string EducationOrganizationId { get; set; }
        public string Type { get; set; }        
        public string Name { get; set; }
        public string StudentUniqueId { get; set; }        
        public string ServiceDescriptor { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public bool IdeaEligibility { get; set; }
        public string IepBeginDate { get; set; }
        public string IepEndDate { get; set; }
        public string IepParentResponse { get; set; }
        public string IepSignatureDate { get; set; }
        public string Eligibility504 { get; set; }
        public string IepReviewDate { get; set; }
        public string IepExitDate { get; set; }
        
        public string lastEvaluationDate { get; set; }
        public bool medicallyFragile { get; set; }
        public bool multiplyDisabled { get; set; }
        public string reasonExitedDescriptor { get; set; }
        public double schoolHoursPerWeek { get; set; }
        public bool servedOutsideOfRegularSession { get; set; }
        public double specialEducationHoursPerWeek { get; set; }
        public string specialEducationSettingDescriptor { get; set; }


        }
    public class UpdateEndDateStudent
    {
        public StudentReference studentReference { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
    }
    public partial class StudentSpecialEducationProgramAssociation
    {
        public int StdId { get; set; }
        public string studentUniqueId { get; set; }
        public string educationOrganizationId { get; set; }
        public string programTypeDescriptorId { get; set; }
        public string programName { get; set; }
        public string programEducationOrganizationId { get; set; }
        public string beginDate { get; set; }
        public string ideaEligibility { get; set; }
        public string specialEducationSettingDescriptorId { get; set; }
        public decimal? specialEducationHoursPerWeek { get; set; }
        public decimal? schoolHoursPerWeek { get; set; }
        public int? multiplyDisabled { get; set; }
        public int? medicallyFragile { get; set; }
        public string lastEvaluationDate { get; set; }
        public string iepReviewDate { get; set; }
        public string iepBeginDate { get; set; }
        public string iepEndDate { get; set; }
        public string iepExitDate { get; set; }
        public string costSharingAgency { get; set; }
        public string parentResponse { get; set; }
        //public string dataSource { get; set; }
        public bool isCostSharing { get; set; }
        public List<Service> relatedServices { get; set; }

    }
    public class EdFiStudentSpecialEducation
    {
        public string id { get; set; }
        public string beginDate { get; set; }
        public EdFiEducationReference educationOrganizationReference { get; set; }
        public ProgramReference programReference { get; set; }
        public StudentReference studentReference { get; set; }
        public string endDate { get; set; }
        public object ideaEligibility { get; set; }
        public string lastEvaluationDate { get; set; }
        public string iepReviewDate { get; set; }
        public string iepBeginDate { get; set; }
        public string iepEndDate { get; set; }
        public string iepExitDate { get; set; }
        
        public object multiplyDisabled { get; set; }
        public object medicallyFragile { get; set; }
        public string reasonExitedDescriptor { get; set; }
        public decimal? schoolHoursPerWeek { get; set; }
        public object specialEducationSettingDescriptor { get; set; }
        public decimal? specialEducationHoursPerWeek { get; set; }
        public List<Service> specialEducationProgramServices { get; set; }

        public EdFiExt _ext { get; set; }
    }
    public class StudentReference
    {
        public string studentUniqueId { get; set; }
        public Link Link { get; set; }
    }

    public class EdFiExt
    {
        public EdFiExt()
        { }
        public Ext myBPS { get; set; }
    }

    public class Ext
    {
        public Ext()
        { }
        public string iepExitDate { get; set; }
        public string costSharingAgency { get; set; }
        public bool isCostSharing { get; set; }
        public string parentResponse { get; set; }
        //public string dataSource { get; set; }
    }
    public class ErrorLog
    {
        public string StudentLocalID { get; set; }
        public string EducationOrganizationId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string ErrorMessage { get; set; }

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
        //public int schoolHoursPerWeek { get; set; }
        //public int specialEducationHoursPerWeek { get; set; }
        public bool servedOutsideOfRegularSession { get; set; }

        public string specialEducationSettingDescriptor { get; set; }
        public List<Service> specialEducationProgramServices { get; set; }

    }





}
