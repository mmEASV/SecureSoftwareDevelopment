using AutoMapper;
using Template.Shared.Dto.Tenant;
using Template.Shared.Dto.User;
using Template.Shared.Models;
using Template.Shared.Models.Identity;
using Template.Shared.Utils;

namespace Template.Api.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<ApplicationUser, UserDto>();
        CreateMap<NewUserDto, ApplicationUser>();
        CreateMap<UpdateUserDto, ApplicationUser>();
        CreateMap<PaginatedList<ApplicationUser>, PaginatedList<UserDto>>()
            .ConvertUsing<PagedListConverter<ApplicationUser, UserDto>>();

        // Tenant
        CreateMap<Tenant, TenantDto>();
        CreateMap<NewTenantDto, Tenant>();
        CreateMap<UpdateTenantDto, Tenant>();
        CreateMap<PaginatedList<Tenant>, PaginatedList<TenantDto>>()
            .ConvertUsing<PagedListConverter<Tenant, TenantDto>>();
    }
}
