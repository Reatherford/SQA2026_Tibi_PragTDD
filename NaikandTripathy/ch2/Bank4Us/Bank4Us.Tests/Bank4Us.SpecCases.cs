using System;
using System.Collections.Generic;
using Bank4Us.Domain;
using Xunit.Abstractions;

namespace Bank4Us.Tests;

internal sealed class SpecCase
{
    public string Name { get; set; }
    public Func<IBankAccountService, (bool ok, string reason)> Run { get; }
    public bool ExpectOk { get; set; }

    public SpecCase(string name, Func<IBankAccountService, (bool ok, string reason)> run, bool expectOk)
    {
        Name = name; Run = run; ExpectOk = expectOk;
    }
}

internal static class SpecSuite
{
    public static List<SpecCase> CreateBaselineSpecs(IAccountRepository repo, IAuthorizer auth, IClock clock)
    {
        var checking = repo.GetById("C1");
        var savings  = repo.GetById("S1");

        return new List<SpecCase>
        {
            new("Deposit: negative rejected", svc =>
            {
                var ok = svc.Deposit("C1", -1m, out var reason); return (ok, reason);
            }, expectOk: false),

            new("Deposit: positive accepted", svc =>
            {
                var ok = svc.Deposit("C1", 100m, out var reason); return (ok, reason);
            }, expectOk: true),

            // NEW: ensure zero deposits are rejected (kills Mutant_AllowsZeroAmount)
            new("Deposit: zero deposit amount rejected", svc =>
            {
                var ok = svc.Deposit("C1", 0m, out var reason); return (ok, reason);
            }, expectOk: false),

            new("Withdraw: exactly balance allowed", svc =>
            {
                repo.GetById("C1").Credit(100m);
                var ok = svc.Withdraw("C1", 100m, out var reason); return (ok, reason);
            }, expectOk: true),

            new("Withdraw: overdraft rejected", svc =>
            {
                // ensure zero balance
                var acct = repo.GetById("C1");
                while (acct.Balance > 0) { acct.Debit(acct.Balance); repo.Save(acct); }
                var ok = svc.Withdraw("C1", 1m, out var reason); return (ok, reason);
            }, expectOk: false),

            // NEW: ensure zero withdrawals are rejected (kills Mutant_AllowsZeroAmount)
            new("Withdraw: zero rejected", svc =>
            {
                var ok = svc.Withdraw("C1", 0m, out var reason); return (ok, reason);
            }, expectOk: false),

            new("Withdraw: frozen account rejected", svc =>
            {
                var acct = repo.GetById("C1");
                acct.Credit(10m); acct.Freeze();
                var ok = svc.Withdraw("C1", 1m, out var reason);
                acct.Unfreeze();
                return (ok, reason);
            }, expectOk: false),

            new("Transfer: at daily limit (10,000) allowed", svc =>
            {
                repo.GetById("C1").Credit(20_000m);
                var ok = svc.Transfer("C1", "S1", 10_000m, out var reason); return (ok, reason);
            }, expectOk: true),

            new("Transfer: just over daily limit rejected", svc =>
            {
                repo.GetById("C1").Credit(20_000m);
                var ok = svc.Transfer("C1", "S1", 0.01m, out var reason); return (ok, reason);
            }, expectOk: false),

            new("Transfer: 6k+4k same day rejected", svc =>
            {
                repo.GetById("C1").Credit(20_000m);
                var o1 = svc.Transfer("C1", "S1", 6_000m, out var r1);
                var o2 = svc.Transfer("C1", "S1", 4_000m, out var r2);
                return (o1 && o2, o1 ? r2 : r1);
            }, expectOk: false),

            new("Transfer: 7k+3001 same day rejected", svc =>
            {
                repo.GetById("C1").Credit(20_000m);
                var o1 = svc.Transfer("C1", "S1", 7_000m, out var r1);
                var o2 = svc.Transfer("C1", "S1", 3_001m, out var r2);
                return (o1 && o2, o1 ? r2 : r1);
            }, expectOk: false),
        };
    }

    public static (int passed, int failed, List<(string name, string reason)> failures)
        ExecuteAll(IBankAccountService svc, IEnumerable<SpecCase> cases, ITestOutputHelper? output = null)
    {
        int passed = 0, failed = 0;
        var failures = new List<(string, string)>();

        foreach (var sc in cases)
        {
            var (ok, reason) = sc.Run(svc);
            var matches = ok == sc.ExpectOk;
            if (!matches)
            {
                failed++;
                failures.Add((sc.Name, reason));
                output?.WriteLine($"[FAIL] {sc.Name} → got: {(ok ? "OK" : "FAIL")} reason='{reason}'");
            }
            else
            {
                passed++;
                output?.WriteLine($"[PASS] {sc.Name}");
            }
        }
        return (passed, failed, failures);
    }
}
