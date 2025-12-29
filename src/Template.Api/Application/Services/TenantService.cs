using AutoMapper;
using Template.Shared.Dto.Tenant;
using Template.Shared.Models;
using Template.Shared.Utils;
using Template.Api.Domain.Common;
using Template.Api.Domain.Exceptions;
using Template.Api.Domain.Interfaces;
using Template.Api.Application.Common.Interfaces;
using Template.Api.Infrastructure.Persistence;

namespace Template.Api.Application.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly CurrentContext _currentContext;
    private readonly IMapper _mapper;

    public TenantService(ApplicationDbContext context, ITenantRepository tenantRepository,
        CurrentContext currentContext, IMapper mapper)
    {
        _context = context;
        _tenantRepository = tenantRepository;
        _currentContext = currentContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<Tenant>> Get(BasePaginator paginator, CancellationToken token)
    {
        return await _tenantRepository.Get(paginator, token);
    }

    public async Task<Tenant?> Get(Guid id)
    {
        var tenant = await _tenantRepository.Get(id);
        if (tenant is null)
        {
            throw new NotFoundException("Tenant not found");
        }

        return tenant;
    }

    public async Task<Tenant> Create(NewTenantDto dto)
    {
        var tenant = _mapper.Map<Tenant>(dto);

        _tenantRepository.Create(tenant);
        await _context.SaveChangesAsync();

        return tenant;
    }

    public async Task<Tenant> Update(Guid id, UpdateTenantDto dto)
    {
        var tenantToUpdate = await _tenantRepository.Get(id);

        if (tenantToUpdate is null)
        {
            throw new NotFoundException("Tenant not found");
        }

        tenantToUpdate = _mapper.Map(dto, tenantToUpdate);

        _tenantRepository.Update(tenantToUpdate);
        await _context.SaveChangesAsync();
        return tenantToUpdate;
    }

    public async Task Delete(Guid id)
    {
        var tenant = await _tenantRepository.Get(id);

        if (tenant is null)
        {
            throw new NotFoundException("Tenant not found");
        }

        _tenantRepository.Delete(tenant);
        await _context.SaveChangesAsync();
    }
}
