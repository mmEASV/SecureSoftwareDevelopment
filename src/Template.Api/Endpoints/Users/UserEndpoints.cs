using AutoMapper;
using Template.Shared.Dto;
using Template.Shared.Dto.User;
using Template.Shared.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Domain.Exceptions;
using Template.Api.Application.Common.Interfaces;

namespace Template.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static RouteGroupBuilder AddUserApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("User");

        api.MapGet("/", GetUsers)
            .WithName("Get Users");

        api.MapGet("/{id:guid}", GetUser)
            .WithName("Get User");

        api.MapPost("/", CreateUser)
            .WithName("Create User");

        api.MapPut("/{id:guid}", UpdateUser)
            .WithName("Update User");

        api.MapDelete("/{id:guid}", DeleteUser)
            .WithName("Delete User");

        return api;
    }

    public static async Task<Results<Ok<PaginatedListDto<UserDto>>, ProblemHttpResult>> GetUsers(
        IUserService userService,
        IMapper mapper,
        [AsParameters] BasePaginator paginator,
        CancellationToken cancellationToken)
    {
        var users = await userService.Get(paginator, cancellationToken);
        var dto = mapper.Map<PaginatedList<UserDto>>(users);

        return TypedResults.Ok(new PaginatedListDto<UserDto>(dto));
    }

    public static async Task<Results<Ok<UserDto>, NotFound, ProblemHttpResult>> GetUser(
        IUserService userService,
        IMapper mapper,
        [FromRoute] Guid id)
    {
        var user = await userService.Get(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        return TypedResults.Ok(mapper.Map<UserDto>(user));
    }

    public static async Task<Results<Created<UserDto>, ProblemHttpResult>> CreateUser(
        IUserService userService,
        IMapper mapper,
        [FromBody] NewUserDto dto)
    {
        var user = await userService.Create(dto);

        return TypedResults.Created("", mapper.Map<UserDto>(user));
    }

    public static async Task<Results<Ok<UserDto>, NotFound, ProblemHttpResult>> UpdateUser(
        IUserService userService,
        IMapper mapper,
        [FromRoute] Guid id,
        [FromBody] UpdateUserDto dto)
    {
        var user = await userService.Update(id, dto);

        return TypedResults.Ok(mapper.Map<UserDto>(user));
    }

    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteUser(
        IUserService userService,
        [FromRoute] Guid id)
    {
        await userService.Delete(id);

        return TypedResults.NoContent();
    }
}
