using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static CheckYourEligibility.Domain.Responses.ApplicationResponse;

namespace CheckYourEligibility.Data.Mappings;

[ExcludeFromCodeCoverage]
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CheckEligibilityRequestData_Fsm, EligibilityCheck>()
       
        .ReverseMap();

        CreateMap<EligibilityCheck, CheckEligibilityItemFsm>()
        .ReverseMap();

        CreateMap<ApplicationRequestData, Application>()
       .ForMember(dest => dest.ParentNationalInsuranceNumber, opt => opt.MapFrom(src => src.ParentNationalInsuranceNumber))
       .ForMember(dest => dest.ParentNationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.ParentNationalAsylumSeekerServiceNumber))
       .ForMember(x => x.ParentDateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.ParentDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
       .ForMember(x => x.ChildDateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.ChildDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
       .ForMember(dest => dest.LocalAuthorityId, opt => opt.Ignore())
       .ForMember(dest => dest.SchoolId, opt => opt.MapFrom(src => src.School))
       .ForMember(dest => dest.School, opt => opt.Ignore())
       .ReverseMap();

        CreateMap<Application, ApplicationResponse>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ApplicationID))
        .ForMember(dest => dest.ParentNationalInsuranceNumber, opt => opt.MapFrom(src => src.ParentNationalInsuranceNumber))
        .ForMember(dest => dest.ParentNationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.ParentNationalAsylumSeekerServiceNumber))
        .ForMember(x => x.ParentDateOfBirth, y => y.MapFrom(z => z.ParentDateOfBirth.ToString("yyyy-MM-dd")))
        .ForMember(x => x.ChildDateOfBirth, y => y.MapFrom(z => z.ChildDateOfBirth.ToString("yyyy-MM-dd")))
        .ForMember(x => x.Status, y => y.MapFrom(z => z.Status.ToString()))
        .ReverseMap();

        CreateMap<Models.School, ApplicationResponse.ApplicationSchool>()
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SchoolId))
             .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.EstablishmentName))
             .ReverseMap();

        CreateMap<LocalAuthority, ApplicationResponse.ApplicationSchool.SchoolLocalAuthority>()
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.LocalAuthorityId))
             .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LaName))
             .ReverseMap();

        
        CreateMap<User, UserData>()
        .ReverseMap();

        CreateMap<User, ApplicationResponse.ApplicationUser>()
            .ReverseMap();

        CreateMap<Audit, AuditData>()
       .ReverseMap();

        CreateMap<EligibilityCheckHash, ApplicationHash>()
                .ReverseMap();

        CreateMap<SystemUser, SystemUserData>()
       .ReverseMap();

    }
}