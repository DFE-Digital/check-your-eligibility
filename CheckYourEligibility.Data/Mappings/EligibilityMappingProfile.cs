using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CheckYourEligibility.Data.Mappings;

[ExcludeFromCodeCoverage]
public class EligibilityMappingProfile : Profile
{
    public EligibilityMappingProfile()
    {
        CreateMap<CheckEligibilityRequestDataFsm, FsmCheckEligibility>()
        .ForMember(dest => dest.NINumber, opt => opt.MapFrom(src => src.NationalInsuranceNumber))
        .ForMember(dest => dest.NASSNumber, opt => opt.MapFrom(src => src.NationalAsylumSeekerServiceNumber))
        .ForMember(x => x.DateOfBirth, y => y.MapFrom(z => DateTime.ParseExact(z.DateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture)))
        .ReverseMap();

        CreateMap<FsmCheckEligibility, CheckEligibilityItemFsm>()
        .ForMember(dest => dest.NationalInsuranceNumber, opt => opt.MapFrom(src => src.NINumber))
        .ForMember(dest => dest.NationalAsylumSeekerServiceNumber, opt => opt.MapFrom(src => src.NASSNumber))
        .ForMember(x => x.DateOfBirth, y => y.MapFrom(z => z.DateOfBirth.ToString("dd/MM/yyyy")))
        .ReverseMap();
    }
}