using AutoMapper;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Models;

namespace UserReportService.Application.Mappings;

public class UserReportMappingProfile : Profile
{
    public UserReportMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForCtorParam("Permissions", opt => opt.MapFrom(s => PermissionCatalog.GetDefaultsForRole(s.Role)));
        CreateMap<DailySalesSummary, DailySalesSummaryDto>()
            .ForCtorParam("TotalNewCustomers", opt => opt.MapFrom(_ => 0));
        CreateMap<MonthlySalesSummary, MonthlySalesSummaryDto>();
    }
}
