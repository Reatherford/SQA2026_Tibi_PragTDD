
using System;

namespace Bank4Us.Domain;

public enum AccountType { PersonalChecking, Savings, BusinessChecking }

public sealed class Account
{
    public string Id { get; }
    public AccountType Type { get; }
    public decimal Balance { get; private set; }
    public bool IsFrozen { get; private set; }

    public Account(string id, AccountType type, decimal openingBalance = 0m, bool isFrozen = false)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Type = type;
        Balance = openingBalance;
        IsFrozen = isFrozen;
    }

    public void Credit(decimal amount) => Balance += amount;
    public void Debit(decimal amount) => Balance -= amount;
    public void Freeze() => IsFrozen = true;
    public void Unfreeze() => IsFrozen = false;
}
