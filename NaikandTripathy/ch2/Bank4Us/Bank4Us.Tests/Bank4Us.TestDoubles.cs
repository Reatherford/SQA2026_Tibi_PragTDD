using System;
using System.Collections.Generic;
using Bank4Us.Domain;
using NSubstitute;

namespace Bank4Us.Tests;

internal static class TestDoubles
{
    public static (IAccountRepository repo, IAuthorizer auth, IClock clock) CreateRepoWith(
        params Account[] accounts)
    {
        var dict = new Dictionary<string, Account>();
        foreach (var a in accounts) dict[a.Id] = a;

        var repo = Substitute.For<IAccountRepository>();
        repo.GetById(Arg.Any<string>()).Returns(ci => dict[(string)ci[0]]);

        // Use When(...).Do(...) for void methods to perform side-effects instead of Returns(...)
        repo.When(r => r.Save(Arg.Any<Account>())).Do(ci =>
        {
            var a = (Account)ci[0];
            dict[a.Id] = a;
        });

        var transfers = new Dictionary<(string, DateOnly), decimal>();
        repo.GetTotalTransfersFor(Arg.Any<string>(), Arg.Any<DateOnly>())
            .Returns(ci =>
            {
                var key = ((string)ci[0], (DateOnly)ci[1]);
                return transfers.TryGetValue(key, out var v) ? v : 0m;
            });

        // Fix: AddDailyTransfer is a void method, so use When(...).Do(...) instead of Returns(...)
        repo.When(r => r.AddDailyTransfer(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<decimal>()))
            .Do(ci =>
            {
                var key = ((string)ci[0], (DateOnly)ci[1]);
                if (!transfers.ContainsKey(key)) transfers[key] = 0m;
                transfers[key] += (decimal)ci[2];
            });

        var auth = Substitute.For<IAuthorizer>();
        auth.AuthorizeTransfer(Arg.Any<Account>(), Arg.Any<Account>(), Arg.Any<decimal>()).Returns(true);

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        return (repo, auth, clock);
    }
}
