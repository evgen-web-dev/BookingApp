using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Exceptions.Auth;
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

public sealed class AuthServiceTests
{
    private const int UserId = 1;
    private const string RawRefreshToken = "some-refresh-token";
    private const string RefreshTokenHash = "some-refresh-token-hash";
    private const string AccessToken = "some-access-token";
    
    private readonly RegisterRequest _registerRequest = new()
    {
        FirstName = "Test First Name",
        LastName = "Test Last Name",
        MiddleName = null,
        DateOfBirth = new DateOnly(1999, 10, 10),
        Email = "test@example.com",
        Password = "Pa$$word1",
        Role = Roles.Client
    };

    private readonly LoginRequest _loginRequest = new()
    {
        Email = "test@example.com",
        Password = "Pa$$word1"
    };
    
    private readonly User _user = new()
    {
        FirstName = "Test First Name",
        LastName = "Test Last Name",
        DateOfBirth = new DateOnly(1999, 10, 10)
    };
    
    private readonly CreateUserResult _createUserResult = new (UserId);
    private readonly AuthenticatedUserResult _authenticatedUserResult;

    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IUserIdentityService> _userIdentityService;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IAccessTokenService> _accessTokenService;
    private readonly Mock<ITokenFamilyService> _tokenFamilyService;
    private readonly Mock<ITokenFamilyRepository> _tokenFamilyRepository;
    private readonly Mock<IRefreshTokenService> _refreshTokenService;
    private readonly Mock<IRefreshTokenRevoker> _refreshTokenRevoker;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly IOptions<TokenFamilyOptions> _tokenFamilyOptions;
    
    private readonly AuthService _authService;
    
    public AuthServiceTests()
    {
        _accessTokenService = new Mock<IAccessTokenService>();
        _logger = new Mock<ILogger<AuthService>>();
        _tokenFamilyOptions = Options.Create(new TokenFamilyOptions { RefreshTokenLifeTimeDays = 3, TokenFamilyAbsoluteLifeTimeDays = 14 });
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _userIdentityService = new Mock<IUserIdentityService>();
        _tokenFamilyService = new Mock<ITokenFamilyService>();
        _tokenFamilyRepository = new Mock<ITokenFamilyRepository>();
        _refreshTokenService = new Mock<IRefreshTokenService>();
        _refreshTokenRevoker = new Mock<IRefreshTokenRevoker>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        
        
        _authenticatedUserResult = new AuthenticatedUserResult()
        {
            Id = UserId, Email = _loginRequest.Email, Roles = [Roles.Client]
        };
        
        // --- RegisterAsync happy path ---
        _mapper
            .Setup(x => x.Map<User>(_registerRequest))
            .Returns(_user); 
        
        _userIdentityService
            .Setup(x => x.CreateAsync(_user, _registerRequest.Password))
            .ReturnsAsync(OperationResult<CreateUserResult>.Success(_createUserResult));

        _userIdentityService
            .Setup(x => x.AddToRoleAsync(_user, _registerRequest.Role))
            .ReturnsAsync(OperationResult.Success()); 
        
        // --- LoginAsync happy path ---
        _userIdentityService
            .Setup(x => x.AuthenticateAsync(_loginRequest.Email, _loginRequest.Password))
            .ReturnsAsync(OperationResult<AuthenticatedUserResult>.Success(_authenticatedUserResult));

        _tokenFamilyRepository
            .Setup(x => x.Add(It.IsAny<TokenFamily>()));

        _refreshTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(RawRefreshToken);
        
        _refreshTokenService
            .Setup(x => x.TryHashRefreshToken(RawRefreshToken, out It.Ref<string>.IsAny))
            .Returns((string _, out string hash) =>
            {
                hash = RefreshTokenHash;
                return true;
            }); 
        
        _refreshTokenRepository
            .Setup(x => x.Add(It.IsAny<RefreshToken>())); 
        
        _accessTokenService
            .Setup(x => x.GenerateAccessToken(_authenticatedUserResult))
            .Returns(AccessToken);
        
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // RollbackAsync is NOT part of the happy path, but it is configured here so the
        // failure tests don't each have to repeat it
        _unitOfWork
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Deliberately NOT configured:
        //  - _unitOfWork.HasActiveTransaction — only read inside the catch block, so each exception test states its own premise explicitly.
        //  - _logger — only used if a rollback itself throws.
        //  - _tokenFamilyService, _refreshTokenRevoker — only used by RefreshAsync/LogoutAsync.
        
        _authService = new AuthService(
            _unitOfWork.Object,
            _userIdentityService.Object,
            _mapper.Object,
            _accessTokenService.Object,
            _tokenFamilyService.Object,
            _tokenFamilyRepository.Object,
            _refreshTokenService.Object,
            _refreshTokenRevoker.Object,
            _refreshTokenRepository.Object,
            _tokenFamilyOptions,
            _logger.Object);
    }
    
    [Fact]
    public async Task RegisterAsync_WhenAllStepsSucceed_ShouldCommitAndReturnUserId()
    {
        var result = await _authService.RegisterAsync(_registerRequest, CancellationToken.None);
        
        result.Succeeded.ShouldBeTrue();
        result.Value.Id.ShouldBe(_createUserResult.Id);
        
        _userIdentityService.Verify(x => x.CreateAsync(_user, _registerRequest.Password), Times.Once);
        _userIdentityService.Verify(x => x.AddToRoleAsync(_user, _registerRequest.Role), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task RegisterAsync_WhenCreateUserFails_ShouldRollbackAndReturnFailure()
    {
        const string error = "Could not create user";
        
        _userIdentityService
            .Setup(x => x.CreateAsync(_user, _registerRequest.Password))
            .ReturnsAsync(OperationResult<CreateUserResult>.Failure([error]));

        var result = await _authService.RegisterAsync(_registerRequest, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldBe([error]); 
        
        _userIdentityService.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task RegisterAsync_WhenAddToRoleFails_ShouldRollbackAndReturnFailure()
    {
        const string error = "Could not assign role to user";
        
        _userIdentityService
            .Setup(x => x.AddToRoleAsync(_user, _registerRequest.Role))
            .ReturnsAsync(OperationResult.Failure([error]));
        
        var result = await _authService.RegisterAsync(_registerRequest, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldBe([error]);
        
        _userIdentityService.Verify(x => x.CreateAsync(_user, _registerRequest.Password), Times.Once);
        _userIdentityService.Verify(x => x.AddToRoleAsync(_user, _registerRequest.Role), Times.Once);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    } 
    
    [Fact]
    public async Task RegisterAsync_WhenExceptionThrownAndTransactionIsActive_ShouldRollbackAndRethrow()
    {
        const string exceptionMessage = "Unexpected error while assigning a role"; 
        
        _userIdentityService
            .Setup(x => x.AddToRoleAsync(_user, _registerRequest.Role))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));
        
        _unitOfWork.Setup(x => x.HasActiveTransaction).Returns(true);
        
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _authService.RegisterAsync(_registerRequest, CancellationToken.None));
        
        exception.Message.ShouldBe(exceptionMessage);
        
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task RegisterAsync_WhenExceptionThrownAndTransactionIsNotActive_ShouldNotRollbackAndRethrow()
    {
        const string exceptionMessage = "Unexpected error while assigning a role";
        
        _userIdentityService
            .Setup(x => x.AddToRoleAsync(_user, _registerRequest.Role))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));
        
        _unitOfWork.Setup(x => x.HasActiveTransaction).Returns(false);
        
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _authService.RegisterAsync(_registerRequest, CancellationToken.None));
        
        exception.Message.ShouldBe(exceptionMessage);
        
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldIssueRefreshAndAccessTokens()
    {
        var result = await _authService.LoginAsync(_loginRequest, CancellationToken.None);
        
        result.Succeeded.ShouldBeTrue();
        result.Value.RefreshToken.ShouldBe(RawRefreshToken);
        result.Value.AccessToken.ShouldBe(AccessToken);
        
        _userIdentityService.Verify(x => x.AuthenticateAsync(_loginRequest.Email, _loginRequest.Password), Times.Once);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tokenFamilyRepository.Verify(x => x.Add(It.IsAny<TokenFamily>()), Times.Once);
        _refreshTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        _refreshTokenService.Verify(x => x.TryHashRefreshToken(RawRefreshToken, out It.Ref<string>.IsAny), Times.Once);
        _refreshTokenRepository.Verify(x => x.Add(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _accessTokenService.Verify(x => x.GenerateAccessToken(_authenticatedUserResult), Times.Once);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenAuthenticationFails_ShouldReturnFailureWithoutOpeningTransaction()
    {
        const string error = "Could not authenticate user";

        _userIdentityService
            .Setup(x => x.AuthenticateAsync(_loginRequest.Email, _loginRequest.Password))
            .ReturnsAsync(OperationResult<AuthenticatedUserResult>.Failure([error]));
        
        var result = await _authService.LoginAsync(_loginRequest, CancellationToken.None);
        
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldBe([error]);
        
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenHashingFailsAndTransactionIsActive_ShouldRollbackAndThrow()
    {
        // Mock returns false per the real contract; AuthService's own guard is what throws.
        _refreshTokenService
            .Setup(x => x.TryHashRefreshToken(RawRefreshToken, out It.Ref<string>.IsAny))
            .Returns(false);
        
        _unitOfWork.Setup(x => x.HasActiveTransaction).Returns(true);
        
        await Should.ThrowAsync<InvalidRefreshTokenHashGenerationException>(
            async () => await _authService.LoginAsync(_loginRequest, CancellationToken.None));
        
        _tokenFamilyRepository.Verify(x => x.Add(It.IsAny<TokenFamily>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task LoginAsync_WhenHashingFailsAndTransactionIsNotActive_ShouldNotRollbackAndThrow()
    {
        _refreshTokenService
            .Setup(x => x.TryHashRefreshToken(RawRefreshToken, out It.Ref<string>.IsAny))
            .Returns(false);
        
        _unitOfWork.Setup(x => x.HasActiveTransaction).Returns(false);
        
        await Should.ThrowAsync<InvalidRefreshTokenHashGenerationException>(
            async () => await _authService.LoginAsync(_loginRequest, CancellationToken.None));
        
        _refreshTokenRepository.Verify(x => x.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}