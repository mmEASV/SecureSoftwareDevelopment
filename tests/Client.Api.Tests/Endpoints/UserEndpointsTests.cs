using AutoMapper;
using Client.Api.Application.Common.Interfaces;
using Client.Api.Application.Common.Mappings;
using Client.Api.Endpoints.Users;
using Client.Shared.Dto;
using Client.Shared.Dto.User;
using Client.Shared.Models.Identity;
using Client.Shared.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Client.Api.Tests.Endpoints;

public class UserEndpointsTests
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public UserEndpointsTests()
    {
        _userService = Substitute.For<IUserService>();

        var myProfile = new MappingProfile();
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile(myProfile));
        _mapper = new Mapper(configuration);
    }

    [Fact]
    public async Task GetUsers_ReturnsOkWithPaginatedList()
    {
        // Arrange
        var users = new PaginatedList<ApplicationUser>(new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "User", Surname = "One" },
            new ApplicationUser { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User", Surname = "Two" }
        }, 1, 2, 1, 10); // totalPages, totalItems, pageIndex, pageSize

        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };
        var cancellationToken = CancellationToken.None;

        _userService.Get(paginator, cancellationToken).Returns(users);

        // Act
        var result = await UserEndpoints.GetUsers(_userService, _mapper, paginator, cancellationToken);

        // Assert
        Assert.IsType<Ok<PaginatedListDto<UserDto>>>(result.Result);
        var okResult = result.Result as Ok<PaginatedListDto<UserDto>>;
        Assert.NotNull(okResult);
        Assert.Equal(2, okResult.Value!.Items.Count);
        Assert.Equal(2, okResult.Value.TotalItems);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsOkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            Name = "Test",
            Surname = "User"
        };

        _userService.Get(userId).Returns(user);

        // Act
        var result = await UserEndpoints.GetUser(_userService, _mapper, userId);

        // Assert
        Assert.IsType<Ok<UserDto>>(result.Result);
        var okResult = result.Result as Ok<UserDto>;
        Assert.NotNull(okResult);
        Assert.Equal(userId, okResult.Value!.Id);
        Assert.Equal("test@test.com", okResult.Value.Email);
    }

    [Fact]
    public async Task CreateUser_WithValidDto_ReturnsCreatedWithUser()
    {
        // Arrange
        var newUserDto = new NewUserDto
        {
            Email = "newuser@test.com",
            Password = "Test@123",
            Name = "New",
            Surname = "User"
        };

        var createdUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = newUserDto.Email,
            Name = newUserDto.Name,
            Surname = newUserDto.Surname
        };

        _userService.Create(newUserDto).Returns(createdUser);

        // Act
        var result = await UserEndpoints.CreateUser(_userService, _mapper, newUserDto);

        // Assert
        Assert.IsType<Created<UserDto>>(result.Result);
        var createdResult = result.Result as Created<UserDto>;
        Assert.NotNull(createdResult);
        Assert.Equal(createdUser.Email, createdResult.Value!.Email);
        Assert.Equal(createdUser.Name, createdResult.Value.Name);
    }

    [Fact]
    public async Task UpdateUser_WithValidId_ReturnsOkWithUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Email = "updated@test.com",
            Name = "Updated",
            Surname = "User"
        };

        var updatedUser = new ApplicationUser
        {
            Id = userId,
            Email = updateDto.Email,
            Name = updateDto.Name,
            Surname = updateDto.Surname
        };

        _userService.Update(userId, updateDto).Returns(updatedUser);

        // Act
        var result = await UserEndpoints.UpdateUser(_userService, _mapper, userId, updateDto);

        // Assert
        Assert.IsType<Ok<UserDto>>(result.Result);
        var okResult = result.Result as Ok<UserDto>;
        Assert.NotNull(okResult);
        Assert.Equal("updated@test.com", okResult.Value!.Email);
        Assert.Equal("Updated", okResult.Value.Name);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await UserEndpoints.DeleteUser(_userService, userId);

        // Assert
        Assert.IsType<NoContent>(result.Result);
        await _userService.Received(1).Delete(userId);
    }

    [Fact]
    public async Task GetUsers_WithPagination_UsesCorrectPaginator()
    {
        // Arrange
        var paginator = new BasePaginator { Page = 2, ItemsPerPage = 5 };
        var users = new PaginatedList<ApplicationUser>(new List<ApplicationUser>(), 0, 2, 2, 5);

        _userService.Get(Arg.Is<BasePaginator>(p => p.Page == 2 && p.ItemsPerPage == 5), Arg.Any<CancellationToken>())
            .Returns(users);

        // Act
        await UserEndpoints.GetUsers(_userService, _mapper, paginator, CancellationToken.None);

        // Assert
        await _userService.Received(1).Get(
            Arg.Is<BasePaginator>(p => p.Page == 2 && p.ItemsPerPage == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUsers_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var emptyUsers = new PaginatedList<ApplicationUser>(new List<ApplicationUser>(), 0, 0, 1, 10); // totalPages, totalItems, pageIndex, pageSize
        var paginator = new BasePaginator { Page = 1, ItemsPerPage = 10 };

        _userService.Get(paginator, Arg.Any<CancellationToken>()).Returns(emptyUsers);

        // Act
        var result = await UserEndpoints.GetUsers(_userService, _mapper, paginator, CancellationToken.None);

        // Assert
        Assert.IsType<Ok<PaginatedListDto<UserDto>>>(result.Result);
        var okResult = result.Result as Ok<PaginatedListDto<UserDto>>;
        Assert.NotNull(okResult);
        Assert.Empty(okResult.Value!.Items);
        Assert.Equal(0, okResult.Value.TotalItems);
    }
}
