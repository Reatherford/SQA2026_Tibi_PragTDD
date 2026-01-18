using System;

namespace Bank4Us.Domain;

/// <summary>
/// Represents a set of bank rules used to validate deposit, withdrawal, and transfer operations.
/// Implementations should return whether an operation is allowed and provide a human-readable reason via the reason desc.
/// </summary>
public interface IBankRuleSet
{
    bool CanDeposit(Account acct, decimal amount, out string reason);

    bool CanWithdraw(Account acct, decimal amount, out string reason);

    bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason);
}

/// <summary>
/// The correct implementation of bank rules:
/// - Deposits must be > 0.
/// - Withdrawals must be > 0, account must not be frozen, and balance must be sufficient.
/// - Transfers delegate withdrawal checks and enforce a per-day limit for personal checking accounts.
/// </summary>
public sealed class CorrectRuleSet : IBankRuleSet
{
    public bool CanDeposit(Account acct, decimal amount, out string reason)         
    {
        if (amount <= 0m) { reason = "Deposit amount must be > 0."; return false; }
        reason = "OK"; return true;
    }

    public bool CanWithdraw(Account acct, decimal amount, out string reason)
    {
        if (amount <= 0m) { reason = "Withdrawal must be > 0."; return false; }
        if (acct.IsFrozen) { reason = "Account is frozen."; return false; }
        if (acct.Balance < amount) { reason = "Insufficient funds."; return false; }
        reason = "OK"; return true;
    }

    public bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason)
    {
        if (!CanWithdraw(from, amount, out reason)) return false;

        if (from.Type == AccountType.PersonalChecking)
        {
            var projected = todaysSoFar + amount;
            if (projected > BankPolicies.PersonalDailyTransferLimit)
            {
                reason = "Daily transfer limit exceeded.";
                return false;
            }
        }
        reason = "OK"; return true;
    }
}

/// <summary>
/// Mutant variant: off-by-one in daily transfer limit check.
/// This mutant rejects transfers when the projected total is equal to or greater than the limit (<= behavior change).
/// </summary>
public sealed class Mutant_OffByOne_Limit : IBankRuleSet
{
    /// <inheritdoc/>
    public bool CanDeposit(Account acct, decimal amount, out string reason)
        => new CorrectRuleSet().CanDeposit(acct, amount, out reason);

    /// <inheritdoc/>
    public bool CanWithdraw(Account acct, decimal amount, out string reason)
        => new CorrectRuleSet().CanWithdraw(acct, amount, out reason);

    /// <summary>
    /// Mutated transfer rule: uses >= instead of > when comparing projected daily transfer total to the limit.
    /// </summary>
    public bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason)
    {
        if (!new CorrectRuleSet().CanWithdraw(from, amount, out reason)) return false;

        if (from.Type == AccountType.PersonalChecking)
        {
            var projected = todaysSoFar + amount;
            if (projected >= BankPolicies.PersonalDailyTransferLimit)
            {
                reason = "Daily transfer limit exceeded.";
                return false;
            }
        }
        reason = "OK"; return true;
    }
}

/// <summary>
/// Mutant variant: missing frozen account check on withdrawals.
/// This mutant allows withdrawals from frozen accounts (missing the frozen check).
/// </summary>
public sealed class Mutant_MissingFrozenCheck : IBankRuleSet
{
    /// <inheritdoc/>
    public bool CanDeposit(Account acct, decimal amount, out string reason)
        => new CorrectRuleSet().CanDeposit(acct, amount, out reason);

    /// <summary>
    /// Mutated withdrawal rule: does not check <see cref="Account.IsFrozen"/> and therefore may allow withdrawals from frozen accounts.
    /// </summary>
    public bool CanWithdraw(Account acct, decimal amount, out string reason)
    {
        if (amount <= 0m) { reason = "Withdrawal must be > 0."; return false; }
        if (acct.Balance < amount) { reason = "Insufficient funds."; return false; }
        reason = "OK"; return true;
    }

    public bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason)
        => new CorrectRuleSet().CanTransfer(from, to, amount, today, todaysSoFar, out reason);
}

/// <summary>
/// Mutant variant: allows zero-amount deposits and withdrawals.
/// This mutant treats zero as valid (>= 0) instead of strictly greater than zero.
/// </summary>
public sealed class Mutant_AllowsZeroAmount : IBankRuleSet
{
    /// <summary>
    /// Mutated deposit rule: permits zero-amount deposits.
    /// </summary>
    public bool CanDeposit(Account acct, decimal amount, out string reason)
    {
        if (amount < 0m) { reason = "Deposit amount must be >= 0 (mutant)."; return false; }
        reason = "OK"; return true;
    }

    /// <summary>
    /// Mutated withdrawal rule: permits zero-amount withdrawals but retains frozen and insufficient funds checks.
    /// </summary>
    public bool CanWithdraw(Account acct, decimal amount, out string reason)
    {
        if (amount < 0m) { reason = "Withdrawal must be ≥ 0 (mutant)."; return false; }
        if (acct.IsFrozen) { reason = "Account is frozen."; return false; }
        if (acct.Balance < amount) { reason = "Insufficient funds."; return false; }
        reason = "OK"; return true;
    }

    /// <inheritdoc/>
    public bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason)
        => new CorrectRuleSet().CanTransfer(from, to, amount, today, todaysSoFar, out reason);
}

/// <summary>
/// Mutant variant: uses logical OR instead of the intended AND conditions in withdrawal checks.
/// This mutant's conditional is overly permissive and may allow withdrawals that should be rejected.
/// </summary>
public sealed class Mutant_OrInsteadOfAnd : IBankRuleSet
{
    /// <inheritdoc/>
    public bool CanDeposit(Account acct, decimal amount, out string reason)
        => new CorrectRuleSet().CanDeposit(acct, amount, out reason);

    /// <summary>
    /// Mutated withdrawal rule: uses OR logic, making the check too permissive.
    /// </summary>
    public bool CanWithdraw(Account acct, decimal amount, out string reason)
    {
        if (amount > 0m || !acct.IsFrozen || acct.Balance >= amount)
        { reason = "OK"; return true; }
        reason = "Mutant incorrectly rejects"; return false;
    }

    public bool CanTransfer(Account from, Account to, decimal amount, DateOnly today, decimal todaysSoFar, out string reason)
        => new CorrectRuleSet().CanTransfer(from, to, amount, today, todaysSoFar, out reason);
}
