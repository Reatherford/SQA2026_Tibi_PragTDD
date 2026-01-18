
using System;

namespace Bank4Us.Domain;

public sealed class BankAccountService : IBankAccountService
{
    private readonly IAccountRepository _repo;
    private readonly IAuthorizer _auth;
    private readonly IClock _clock;
    private readonly IBankRuleSet _rules;

    public BankAccountService(IAccountRepository repo, IAuthorizer auth, IClock clock, IBankRuleSet? rules = null)
    {
        _repo = repo;
        _auth = auth;
        _clock = clock;
        _rules = rules ?? new CorrectRuleSet();
    }

    public bool Deposit(string accountId, decimal amount, out string reason)
    {
        var acct = _repo.GetById(accountId);
        if (!_rules.CanDeposit(acct, amount, out reason)) return false;
        acct.Credit(amount);
        _repo.Save(acct);
        return true;
    }

    public bool Withdraw(string accountId, decimal amount, out string reason)
    {
        var acct = _repo.GetById(accountId);
        if (!_rules.CanWithdraw(acct, amount, out reason)) return false;
        acct.Debit(amount);
        _repo.Save(acct);
        return true;
    }

    public bool Transfer(string fromAccountId, string toAccountId, decimal amount, out string reason)
    {
        var from = _repo.GetById(fromAccountId);
        var to = _repo.GetById(toAccountId);
        if (!_auth.AuthorizeTransfer(from, to, amount))
        { reason = "Authorization failed."; return false; }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var todaysSoFar = _repo.GetTotalTransfersFor(from.Id, today);

        if (!_rules.CanTransfer(from, to, amount, today, todaysSoFar, out reason)) return false;

        from.Debit(amount);
        to.Credit(amount);
        _repo.AddDailyTransfer(from.Id, today, amount);
        _repo.Save(from);
        _repo.Save(to);
        return true;
    }
}
