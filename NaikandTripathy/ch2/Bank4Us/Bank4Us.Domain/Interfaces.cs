
using System;

namespace Bank4Us.Domain;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IAuthorizer
{
    bool AuthorizeTransfer(Account from, Account to, decimal amount);
}

public interface IAccountRepository
{
    Account GetById(string id);
    void Save(Account account);

    decimal GetTotalTransfersFor(string accountId, DateOnly day);
    void AddDailyTransfer(string accountId, DateOnly day, decimal amount);
}

public interface IBankAccountService
{
    bool Deposit(string accountId, decimal amount, out string reason);
    bool Withdraw(string accountId, decimal amount, out string reason);
    bool Transfer(string fromAccountId, string toAccountId, decimal amount, out string reason);
}
