using AccountService.Features.Transactions.Commands.CreateTransaction;
using AutoMapper;

namespace AccountService.Features.Transactions.Models;

public class TransactionMapperProfile : Profile
{
    public TransactionMapperProfile()
    {
        CreateMap<Transaction, TransactionDto>();
        CreateMap<TransactionDto, Transaction>();

        CreateMap<CreateTransactionCommand, Transaction>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}