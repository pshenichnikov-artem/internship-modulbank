using AccountService.Features.Accounts.Commands.CreateAccount;
using AccountService.Features.Accounts.Commands.UpdateAccount;
using AutoMapper;

namespace AccountService.Features.Accounts.Model;

public class AccountMapperProfile : Profile
{
    public AccountMapperProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<AccountDto, Account>();

        CreateMap<CreateAccountCommand, Account>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.OpenedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ClosedAt, opt => opt.Ignore());

        CreateMap<UpdateAccountCommand, Account>()
            .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.Balance, opt => opt.Ignore())
            .ForMember(dest => dest.OpenedAt, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
    }
}