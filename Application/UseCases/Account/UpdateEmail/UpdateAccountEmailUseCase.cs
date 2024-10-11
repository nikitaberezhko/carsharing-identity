using Application.Application.Interfaces;
using Application.Infrastructure.Interfaces.Repositories;
using ApplicationException = Application.Exceptions.ApplicationException;

namespace Application.UseCases.Account.UpdateEmail;

public class UpdateAccountEmailUseCase(IAccountRepository accountRepository) 
    : IUseCase<UpdateAccountEmailRequest, UpdateAccountEmailResponse>
{
    public async Task<UpdateAccountEmailResponse> Execute(UpdateAccountEmailRequest request)
    {
        // todo: this case is invalid
        var account = await accountRepository.GetById(request.AccountId);
        if (account != null)
        {
            account.SetEmail(request.Email);
            return new UpdateAccountEmailResponse(account.Email);
        }
        
        throw new ApplicationException("Account not found",
            $"Account with this id: {request.AccountId} not found");
    }
}