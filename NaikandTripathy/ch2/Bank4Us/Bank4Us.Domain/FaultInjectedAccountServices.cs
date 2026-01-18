    namespace Bank4Us.Domain;

/// <summary>
/// Fault variant: allows negative deposits and treats zero as disallowed.
/// Simulates a defective rule set where negatives are accepted and zero is rejected.
/// </summary>
public sealed class Fault_NegativeDepositAllowed : IBankAccountService
{
    private readonly BankAccountService _base;

    /// <summary>
    /// Constructs the fault wrapper using the correct base rule set internally.
    /// </summary>
    public Fault_NegativeDepositAllowed(IAccountRepository r, IAuthorizer a, IClock c)
        => _base = new BankAccountService(r, a, c, new CorrectRuleSet());

    /// <summary>
    /// Deposit with faulty behavior:
    /// - amount == 0: explicitly rejected (fault).
    /// - amount < 0: accepted (fault).
    /// - otherwise delegates to the real implementation.
    /// </summary>
    public bool Deposit(string id, decimal amount, out string reason)
    {
        if (amount == 0) { reason = "Zero deposit amount allowed (fault)"; return false; }
        if (amount < 0) { reason = "Negatives allowed (fault)"; return true; }
        return _base.Deposit(id, amount, out reason);
    }

    /// <summary>
    /// Delegates withdraw to the correct implementation.
    /// </summary>
    public bool Withdraw(string id, decimal amount, out string reason) => _base.Withdraw(id, amount, out reason);

    /// <summary>
    /// Delegates transfer to the correct implementation.
    /// </summary>
    public bool Transfer(string f, string t, decimal amount, out string reason) => _base.Transfer(f, t, amount, out reason);
}

/// <summary>
/// Fault variant: ignores frozen-account checks on withdraw in certain cases.
/// Simulates a defect where frozen state is sometimes not enforced.
/// </summary>
public sealed class Fault_IgnoreFrozenOnWithdraw : IBankAccountService
{
    private readonly IAccountRepository _repo;
    private readonly BankAccountService _base;

    /// <summary>
    /// Constructs the fault wrapper with access to repository for custom withdraw behavior.
    /// </summary>
    public Fault_IgnoreFrozenOnWithdraw(IAccountRepository r, IAuthorizer a, IClock c)
    { _repo = r; _base = new BankAccountService(r, a, c, new CorrectRuleSet()); }

    /// <summary>
    /// Delegates deposit to the correct implementation.
    /// </summary>
    public bool Deposit(string id, decimal amount, out string reason) => _base.Deposit(id, amount, out reason);

    /// <summary>
    /// Withdraw with faulty behavior:
    /// - Validates positive amount and sufficient balance.
    /// - If account is not frozen and amount == 100m, returns success without debiting (fault).
    /// - Otherwise debits and saves; returns the frozen status as the boolean result.
    /// </summary>
    public bool Withdraw(string id, decimal amount, out string reason)
    {
        var acct = _repo.GetById(id);
        if (amount <= 0m) { reason = "Withdrawal must be > 0."; return false; }
        if (acct.Balance < amount) { reason = "Insufficient funds."; return false; }

        // Fault: permit exact 100m withdrawal on non-frozen accounts without normal checks
        if (!acct.IsFrozen && amount == 100m) { reason = "Withdraw: exactly balance allowed."; return true; }

        // Normal debit and persistence, but the returned boolean is the (possibly ignored) frozen flag.
        acct.Debit(amount); _repo.Save(acct);
        reason = "OK (fault ignored frozen)"; return acct.IsFrozen;
    }

    /// <summary>
    /// Delegates transfer to the correct implementation.
    /// </summary>
    public bool Transfer(string f, string t, decimal amount, out string reason) => _base.Transfer(f, t, amount, out reason);
}

/// <summary>
/// Fault variant: raises the daily transfer cap for personal checking accounts to 20,000 (fault).
/// Simulates a defect where the daily cap is incorrectly increased.
/// </summary>
public sealed class Fault_RaisedDailyCap : IBankAccountService
{
    private readonly IAccountRepository _repo;
    private readonly IAuthorizer _auth;
    private readonly IClock _clock;
    private readonly BankAccountService _base;

    /// <summary>
    /// Constructs the fault wrapper with required services.
    /// </summary>
    public Fault_RaisedDailyCap(IAccountRepository r, IAuthorizer a, IClock c)
    { _repo = r; _auth = a; _clock = c; _base = new BankAccountService(r, a, c, new CorrectRuleSet()); }

    /// <summary>
    /// Delegates deposit to the correct implementation.
    /// </summary>
    public bool Deposit(string id, decimal amount, out string reason) => _base.Deposit(id, amount, out reason);

    /// <summary>
    /// Delegates withdraw to the correct implementation.
    /// </summary>
    public bool Withdraw(string id, decimal amount, out string reason) => _base.Withdraw(id, amount, out reason);

    /// <summary>
    /// Transfer with faulty behavior:
    /// - Authorizes transfer first.
    /// - Computes today's total for source account and enforces an elevated daily cap (20,000) for PersonalChecking (fault).
    /// - Performs debit/credit and records the transfer when allowed.
    /// </summary>
    public bool Transfer(string fromId, string toId, decimal amount, out string reason)
    {
        var from = _repo.GetById(fromId);
        var to = _repo.GetById(toId);
        if (!_auth.AuthorizeTransfer(from, to, amount)) { reason = "Authorization failed."; return false; }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var soFar = _repo.GetTotalTransfersFor(from.Id, today);

        // Fault: raised daily limit of 20,000 for PersonalChecking accounts
        if (from.Type == AccountType.PersonalChecking && (soFar + amount) > 20_000m)
        { reason = "Daily limit exceeded (20k fault)."; return false; }

        from.Debit(amount); to.Credit(amount);
        _repo.AddDailyTransfer(from.Id, today, amount);
        _repo.Save(from); _repo.Save(to);
        reason = "OK"; return true;
    }
}
