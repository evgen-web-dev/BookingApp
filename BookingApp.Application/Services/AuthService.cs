using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Mapster;

namespace BookingApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    
    public AuthService(IUnitOfWork unitOfWork, IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }
    
    public async Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Roles.RolesAvailableForPublicRegistration.Contains(request.Role))
        {
            return new AuthResult<RegisterResponse>(
                false,
                ["Could not create account", $"Invalid role provided: {request.Role}"]
            );
        }

        User userFromMappedRequest = request.Adapt<User>();
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var isCommitted = false;
        
        try
        {
            var createUserResult = await _userRepository.CreateAsync(userFromMappedRequest, request.Password);
            if (!createUserResult.Succeeded)
            {
                return new AuthResult<RegisterResponse>(false, createUserResult.Errors.ToList());
            }

            var assignUserToRole = await _userRepository.AddToRoleAsync(userFromMappedRequest, request.Role);
            if (!assignUserToRole.Succeeded)
            {
                return new AuthResult<RegisterResponse>(false, assignUserToRole.Errors.ToList());
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            isCommitted = true;

            return new AuthResult<RegisterResponse>(true, [], new RegisterResponse(createUserResult.Value.Id));
        }
        finally
        {
            if (!isCommitted)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }
        }
    }

    public async Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        
        return new AuthResult<LoginResponse>(true);
    }
}