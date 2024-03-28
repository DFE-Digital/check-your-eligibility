using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CheckYourEligibility.Data.Mappings;

[ExcludeFromCodeCoverage]
public class FsmMappingProfile : Profile
{
    public FsmMappingProfile()
    {
        CreateMap<CheckEligibilityRequestDataFsm, EligibilityCheck>()
        .ForMember(dest => dest.NINumber, opt => opt.MapFrom(src => src.NationalInsuranceNumber))
        .ForMember(dest => dest.NASSNumber, opt => opt.MapFrom(src => src.NationalAsylumSeekerServiceNumber))
        .ForMember(x => x.DateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.DateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture)))
        .ReverseMap();

        CreateMap<EligibilityCheck, CheckEligibilityItemFsm>()
        .ForMember(dest => dest.NationalInsuranceNumber, opt => opt.MapFrom(src => src.NINumber))
        .ForMember(dest => dest.NationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.NASSNumber))
        .ForMember(x => x.DateOfBirth, y => y.MapFrom(z => z.DateOfBirth.ToString("dd/MM/yyyy")))
        .ReverseMap();

        CreateMap<ApplicationRequestDataFsm, Application>()
       .ForMember(dest => dest.ParentNationalInsuranceNumber, opt => opt.MapFrom(src => src.ParentNationalInsuranceNumber))
       .ForMember(dest => dest.ParentNationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.ParentNationalAsylumSeekerServiceNumber))
       .ForMember(x => x.ParentDateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.ParentDateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture)))
       .ForMember(x => x.ChildDateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.ChildDateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture)))
       .ForMember(dest => dest.LocalAuthorityId, opt => opt.Ignore())
       .ForMember(dest => dest.SchoolId, opt => opt.MapFrom(src => src.School))
       .ForMember(dest => dest.School, opt => opt.Ignore())
       .ReverseMap();

        CreateMap<Application, ApplicationSaveFsm>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ApplicationID))
        .ForMember(dest => dest.ParentNationalInsuranceNumber, opt => opt.MapFrom(src => src.ParentNationalInsuranceNumber))
        .ForMember(dest => dest.ParentNationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.ParentNationalAsylumSeekerServiceNumber))
        .ForMember(x => x.ParentDateOfBirth, y => y.MapFrom(z => z.ParentDateOfBirth.ToString("dd/MM/yyyy")))
        .ForMember(x => x.ChildDateOfBirth, y => y.MapFrom(z => z.ChildDateOfBirth.ToString("dd/MM/yyyy")))
        .ForMember(dest => dest.LocalAuthority, opt => opt.MapFrom(src => src.LocalAuthorityId))
        .ForMember(dest => dest.School, opt => opt.MapFrom(src => src.SchoolId))
        .ReverseMap();

        CreateMap<Application, ApplicationFsm>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ApplicationID))
        .ForMember(dest => dest.ParentNationalInsuranceNumber, opt => opt.MapFrom(src => src.ParentNationalInsuranceNumber))
        .ForMember(dest => dest.ParentNationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.ParentNationalAsylumSeekerServiceNumber))
        .ForMember(x => x.ParentDateOfBirth, y => y.MapFrom(z => z.ParentDateOfBirth.ToString("dd/MM/yyyy")))
        .ForMember(x => x.ChildDateOfBirth, y => y.MapFrom(z => z.ChildDateOfBirth.ToString("dd/MM/yyyy")))
        .ForMember(dest => dest.School, opt => opt.Ignore())
        .ReverseMap();
    }
}