using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Template.Shared.Dto.User;
using Template.Shared.Models;
using Template.Shared.Utils;
using Template.Api.Domain.Common;
using Template.Api.Domain.Exceptions;
using Template.Api.Domain.Interfaces;
using Template.Api.Application.Common.Interfaces;
using Template.Api.Infrastructure.Persistence;
using Template.Shared.Models.Identity;

namespace Template.Api.Application.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IMapper mapper, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<PaginatedList<ApplicationUser>> Get(BasePaginator paginator, CancellationToken token)
    {
        return await _userRepository.Get(paginator, token);
    }

    public async Task<ApplicationUser?> Get(Guid id)
    {
        var user = await _userRepository.Get(id);
        if (user is null)
        {
            throw new NotFoundException("User not found");
        }

        return user;
    }

    public async Task<ApplicationUser> Create(NewUserDto dto)
    {
        // Validate tenant if provided
        if (dto.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.Get(dto.TenantId.Value);
            if (tenant is null)
            {
                throw new BadRequestException("Specified tenant does not exist");
            }
        }

        var user = _mapper.Map<ApplicationUser>(dto);

        await _userManager.CreateAsync(user, dto.Password);

        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<ApplicationUser> Update(Guid id, UpdateUserDto dto)
    {
        var userToUpdate = await _userRepository.Get(id);

        if (userToUpdate is null)
        {
            throw new NotFoundException("User not found");
        }

        // Validate tenant if provided
        if (dto.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.Get(dto.TenantId.Value);
            if (tenant is null)
            {
                throw new BadRequestException("Specified tenant does not exist");
            }
        }

        userToUpdate = _mapper.Map(dto, userToUpdate);

        await _userManager.UpdateAsync(userToUpdate);

        await _context.SaveChangesAsync();
        return userToUpdate;
    }

    public async Task Delete(Guid id)
    {
        var user = await _userRepository.Get(id);

        if (user is null)
        {
            throw new NotFoundException("User not found");
        }

        await _userManager.DeleteAsync(user);

        await _context.SaveChangesAsync();
    }
}
