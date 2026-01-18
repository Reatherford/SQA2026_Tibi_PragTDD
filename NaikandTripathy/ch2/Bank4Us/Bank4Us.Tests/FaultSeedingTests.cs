using System.Collections.Generic;
using System.Linq;
using Bank4Us.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Bank4Us.Tests;

/// <summary>
/// Tests that verify seeded faulty implementations of <see cref="IBankAccountService"/>
/// are detected by the specification suite.
/// 
/// This class:
/// - Creates small test worlds with known accounts.
/// - Supplies a set of intentionally faulty service implementations (seeds).
/// - Runs the specification suite against each seeded service and asserts that
///   the suite detects each fault (i.e., at least one spec fails).
/// </summary>
public sealed class FaultSeedingTests
{
    private readonly ITestOutputHelper _out;
    public FaultSeedingTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// Create a fresh repository, authorizer and clock seeded with two accounts.
    /// Used to ensure each seeded fault runs against an identical starting state.
    /// </summary>
    private static (IAccountRepository repo, IAuthorizer auth, IClock clock)
        NewWorld()
        => TestDoubles.CreateRepoWith(
            new Account("C1", AccountType.PersonalChecking, 0m),
            new Account("S1", AccountType.Savings, 0m)
        );

    /// <summary>
    /// Enumerates tuples of (name, faulty service, repo, authorizer, clock).
    /// Each yielded service intentionally violates a spec so the test asserts detection.
    /// </summary>
    private IEnumerable<(string name, IBankAccountService svc, IAccountRepository repo, IAuthorizer auth, IClock clock)>
        SeededFaultServices()
    {
        {
            var (repo, auth, clock) = NewWorld();
            yield return ("Fault_NegativeDepositAllowed",
                new Fault_NegativeDepositAllowed(repo, auth, clock), repo, auth, clock);
        }
        //{
        //    var (repo, auth, clock) = NewWorld();
        //    yield return ("Fault_IgnoreFrozenOnWithdraw",
        //        new Fault_IgnoreFrozenOnWithdraw(repo, auth, clock), repo, auth, clock);
        //}
        //{
        //    var (repo, auth, clock) = NewWorld();
        //    yield return ("Fault_RaisedDailyCap",
        //        new Fault_RaisedDailyCap(repo, auth, clock), repo, auth, clock);
        //}
    }

    /// <summary>
    /// Runs the full specification suite against each seeded faulty service and reports detection rate.
    /// The test asserts that every seeded fault is detected (no survivors).
    /// </summary>
    [Fact]
    public void FaultSeeding_Should_Report_Detection_Rate()
    {
        var seeds = SeededFaultServices().ToList();
        int total = seeds.Count, killed = 0;

        foreach (var (name, svc, repo, auth, clock) in seeds)
        {
            var cases = SpecSuite.CreateBaselineSpecs(repo, auth, clock);
            var (passed, failed, failures) = SpecSuite.ExecuteAll(svc, cases, _out);

            bool detected = failed > 0;
            if (detected) killed++;

            _out.WriteLine($"Seeded Fault: {name} → {(detected ? "DETECTED" : "NOT detected")}");
            if (!detected) _out.WriteLine("  Survivors had no spec mismatches!");
        }

        var detectionRate = (double)killed / total * 100.0;
        _out.WriteLine($"Fault Seeding: killed {killed}/{total} → {detectionRate:F1}%");

        Assert.True(killed == total, $"Some seeded faults survived: killed={killed}, total={total}");
    }
}
