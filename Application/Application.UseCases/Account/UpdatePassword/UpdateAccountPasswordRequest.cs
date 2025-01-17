namespace Application.Application.UseCases.Account.UpdatePassword;

public record UpdateAccountPasswordRequest(
    string NewPassword,
    Guid AccountId,
    Guid ResetToken);