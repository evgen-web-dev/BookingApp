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
        var mockedUser = new User
        {
            FirstName = "Test First Name",
            LastName = "Test Last Name",
            DateOfBirth = new DateOnly(1999, 10,10)
        };
        var mockedRequest = new RegisterRequest
        {
            FirstName = "Test First Name",
            LastName = "Test Last Name",
            DateOfBirth = new DateOnly(1999, 10,10),
            Email = "test@example.com",
            Password = "Pa$$word1",
            Role = Roles.Client
        };
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
        
        // mocking ILogger
        var mockedLogger = new Mock<ILogger<AuthService>>(MockBehavior.Strict);
        
        var authService = BuildAuthService(
            unitOfWork: mockedUnitOfWork.Object,
            userIdentityService: mockedUserIdentityService.Object,
            mapper: mockedMapper.Object,
            logger: mockedLogger.Object);

        var registerResult = await authService.RegisterAsync(mockedRequest, CancellationToken.None);
        
        registerResult.Succeeded.ShouldBeTrue();
        registerResult.Value.Id.ShouldBe(mockedCreateUserResult.Id);
        
        mockedUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
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