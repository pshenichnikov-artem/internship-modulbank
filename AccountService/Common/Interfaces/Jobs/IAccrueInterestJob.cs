namespace AccountService.Common.Interfaces.Jobs;

public interface IAccrueInterestJob
{
    Task ExecuteAsync();
}