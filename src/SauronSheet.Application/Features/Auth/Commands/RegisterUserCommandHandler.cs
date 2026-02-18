namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using Domain.Exceptions;
using DTOs;
using MediatR;

/// <summary>
/// Handler for RegisterUserCommand.
/// Validates input, calls IAuthService.RegisterAsync, returns RegistrationResultDto.
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegistrationResultDto>
{
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<RegistrationResultDto> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Validation
        if (request.Password != request.ConfirmPassword)
            throw new DomainException("Passwords do not match.");

        if (request.Password.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");

        // Call infrastructure service
        var result = await _authService.RegisterAsync(request.Email, request.Password);

        if (!result.IsSuccess)
            throw new DomainException(result.ErrorMessage ?? "Registration failed.");

        return new RegistrationResultDto(result.UserId!.Value, request.Email);
    }
}
