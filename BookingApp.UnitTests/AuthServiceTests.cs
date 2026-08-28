using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Application.Services;
using BookingApp.Domain;
using BookingApp.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace BookingApp.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task AuthService_RegisterAsync_WhenRegisterRequestIsValidAndNoExceptionsThrown_ShouldCreateUser()
    {
        var mockedUser = BuildMockedUser();
        var mockedRequest = BuildMockedRegisterRequest();
        var mockedCreateUserResult = new CreateUserResult(1);
        
        // mocking UoW
        var mockedUnitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        mockedUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockedUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockedUnitOfWork.Setup(x => x.HasActiveTransaction)
            .Returns(false);

        // mocking Mapster mapper
        var mockedMapper = new Mock<IMapper>(MockBehavior.Strict);
        mockedMapper.Setup(x => x.Map<User>(mockedRequest))
            .Returns(mockedUser);
        
        // mocking IUserIdentityService
        var mockedUserIdentityService = new Mock<IUserIdentityService>(MockBehavior.Strict);
        mockedUserIdentityService.Setup(x => x.CreateAsync(mockedUser, mockedRequest.Password))
            .ReturnsAsync(OperationResult<CreateUserResult>.Success(mockedCreateUserResult));
        
        mockedUserIdentityService.Setup(x => x.AddToRoleAsync(mockedUser, mockedRequest.Role))
            .ReturnsAsync(OperationResult.Success());
        
        // not configuring mock for ILogger
        // cause it is not expected to be called in the scope that this test covers
        // (if it will be called in real AuthService.RegisterAsync - test will throw anyway) 
        
        var authService = BuildAuthService(
            unitOfWork: mockedUnitOfWork.Object,
            userIdentityService: mockedUserIdentityService.Object,
            mapper: mockedMapper.Object);

        var registerResult = await authService.RegisterAsync(mockedRequest, CancellationToken.None);
        
        registerResult.Succeeded.ShouldBeTrue();
        registerResult.Value.Id.ShouldBe(mockedCreateUserResult.Id);
        
        mockedUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task AuthService_RegisterAsync_WhenUserIdentityServiceCreateAsyncFails_ShouldNotCreateUser()
    {
        var mockedUser = BuildMockedUser();
        var mockedRequest = BuildMockedRegisterRequest();
        const string couldNotCreateUserErrorMessage = "Could not create user";
        
        // mocking UoW
        var mockedUnitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        mockedUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockedUnitOfWork
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // not configuring mock for mockedUnitOfWork.CommitAsync and mockedUnitOfWork.HasActiveTransaction
        // cause they are not expected to be called in the scope that this test covers
        // (if either of them will be called in real AuthService.RegisterAsync - test will throw anyway) 

        // mocking Mapster mapper
        var mockedMapper = new Mock<IMapper>(MockBehavior.Strict);
        mockedMapper
            .Setup(x => x.Map<User>(mockedRequest))
            .Returns(mockedUser);
        
        // mocking IUserIdentityService
        var mockedUserIdentityService = new Mock<IUserIdentityService>(MockBehavior.Strict);
        mockedUserIdentityService
            .Setup(x => x.CreateAsync(mockedUser, mockedRequest.Password))
            .ReturnsAsync(OperationResult<CreateUserResult>.Failure([couldNotCreateUserErrorMessage]));
        
        // not configuring mocks for mockedUserIdentityService.AddToRoleAsync
        // cause it is not expected to be called in the scope that this test covers
        // (if it will be called in real AuthService.RegisterAsync - test will throw anyway) 
        
        // not configuring mock for ILogger
        // cause it is not expected to be called in the scope that this test covers
        // (if it will be called in real AuthService.RegisterAsync - test will throw anyway) 
        
        var authService = BuildAuthService(
            unitOfWork: mockedUnitOfWork.Object,
            userIdentityService: mockedUserIdentityService.Object,
            mapper: mockedMapper.Object);

        var registerResult = await authService.RegisterAsync(mockedRequest, CancellationToken.None);
        
        registerResult.Succeeded.ShouldBeFalse();
        registerResult.Errors.ShouldBe([couldNotCreateUserErrorMessage]);
        mockedUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task AuthService_RegisterAsync_WhenUserIdentityServiceAddToRoleFails_ShouldNotCreateUser()
    {
        var mockedUser = BuildMockedUser();
        var mockedRequest = BuildMockedRegisterRequest();
        const string couldNotCreateUserWithoutARoleErrorMessage = "Could not create user without a role";
        
        // mocking UoW
        var mockedUnitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        mockedUnitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockedUnitOfWork
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // not configuring mock for mockedUnitOfWork.CommitAsync and mockedUnitOfWork.HasActiveTransaction
        // cause they are not expected to be called in the scope that this test covers
        // (if either of them will be called/reached in real AuthService.RegisterAsync - test will throw anyway) 

        // mocking Mapster mapper
        var mockedMapper = new Mock<IMapper>(MockBehavior.Strict);
        mockedMapper
            .Setup(x => x.Map<User>(mockedRequest))
            .Returns(mockedUser);
        
        // mocking IUserIdentityService
        var mockedUserIdentityService = new Mock<IUserIdentityService>(MockBehavior.Strict);
        mockedUserIdentityService
            .Setup(x => x.CreateAsync(mockedUser, mockedRequest.Password))
            .ReturnsAsync(OperationResult<CreateUserResult>.Success(new CreateUserResult(2)));
        
        mockedUserIdentityService.Setup(x => x.AddToRoleAsync(mockedUser, mockedRequest.Role))
            .ReturnsAsync(OperationResult.Failure([couldNotCreateUserWithoutARoleErrorMessage]));
        
        // not configuring mock for ILogger
        // cause it is not expected to be called in the scope that this test covers
        // (if it will be called in real AuthService.RegisterAsync - test will throw anyway) 
        
        var authService = BuildAuthService(
            unitOfWork: mockedUnitOfWork.Object,
            userIdentityService: mockedUserIdentityService.Object,
            mapper: mockedMapper.Object);

        var registerResult = await authService.RegisterAsync(mockedRequest, CancellationToken.None);
        
        registerResult.Succeeded.ShouldBeFalse();
        registerResult.Errors.ShouldBe([couldNotCreateUserWithoutARoleErrorMessage]);
        
        mockedUserIdentityService.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        mockedUserIdentityService.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        
        mockedUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User BuildMockedUser()
    {
        return new User
        {
            FirstName = "Test First Name",
            LastName = "Test Last Name",
            DateOfBirth = new DateOnly(1999, 10,10)
        };
    }
    
    private static RegisterRequest BuildMockedRegisterRequest()
    {
        return new RegisterRequest
        {
            FirstName = "Test First Name",
            LastName = "Test Last Name",
            DateOfBirth = new DateOnly(1999, 10,10),
            Email = "test@example.com",
            Password = "Pa$$word1",
            Role = Roles.Client
        };
    }
    
    private static IAuthService BuildAuthService(
        IUnitOfWork? unitOfWork = null,
        IUserIdentityService? userIdentityService = null, 
        IMapper? mapper = null,
        IAccessTokenService? accessTokenService = null, 
        ITokenFamilyService? tokenFamilyService = null, 
        ITokenFamilyRepository? tokenFamilyRepository = null, 
        IRefreshTokenService? refreshTokenService = null,
        IRefreshTokenRevoker? refreshTokenRevoker = null,
        IRefreshTokenRepository? refreshTokenRepository = null, 
        IOptions<TokenFamilyOptions>? tokenFamilyOptions = null,
        ILogger<AuthService>? logger = null
        )
    {
        return new AuthService(
            unitOfWork ?? new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            userIdentityService ?? new Mock<IUserIdentityService>(MockBehavior.Strict).Object,
            mapper ?? new Mock<IMapper>(MockBehavior.Strict).Object,
            accessTokenService ?? new Mock<IAccessTokenService>(MockBehavior.Strict).Object,
            tokenFamilyService ?? new Mock<ITokenFamilyService>(MockBehavior.Strict).Object,
            tokenFamilyRepository ?? new Mock<ITokenFamilyRepository>(MockBehavior.Strict).Object,
            refreshTokenService ?? new Mock<IRefreshTokenService>(MockBehavior.Strict).Object,
            refreshTokenRevoker ?? new Mock<IRefreshTokenRevoker>(MockBehavior.Strict).Object,
            refreshTokenRepository ?? new Mock<IRefreshTokenRepository>(MockBehavior.Strict).Object,
            tokenFamilyOptions ?? Options.Create(new TokenFamilyOptions { RefreshTokenLifeTimeDays = 3, TokenFamilyAbsoluteLifeTimeDays = 14 }),
            logger ?? new Mock<ILogger<AuthService>>(MockBehavior.Strict).Object);
    }
}