using System.Linq;
using Bank4Us.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Bank4Us.Tests;

/// <summary>
/// Test suite runner that verifies the reference implementation satisfies the specification suite.
/// Brief explanations are provided for setup and the verification test.
/// </summary>
public sealed class SpecTests
{
    private readonly ITestOutputHelper _out;

    // ITestOutputHelper is injected by xUnit to capture test output.
    public SpecTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// Create a new test world with test doubles and a service under test.
    /// Returns:
    ///  - svc: the BankAccountService to exercise
    ///  - repo: an in-memory account repository pre-seeded with sample accounts
    ///  - auth: an authorizer double used by the service
    ///  - clock: a controllable clock double used to deterministically test time-dependent logic
    /// </summary>
    private static (IBankAccountService svc, IAccountRepository repo, IAuthorizer auth, IClock clock)
        NewWorld()
    {
        // Seed repository with two accounts:
        // - "C1": a personal checking account used for common operations
        // - "S1": a savings account used to exercise savings-specific rules
        var (repo, auth, clock) = TestDoubles.CreateRepoWith(
            new Account("C1", AccountType.PersonalChecking, 0m),
            new Account("S1", AccountType.Savings, 0m)
        );

        // Construct the reference service implementation with the correct rule set.
        var svc = new BankAccountService(repo, auth, clock, new CorrectRuleSet());

        // Return all collaborators so tests can drive scenarios and inspect outcomes.
        return (svc, repo, auth, clock);
    }

    /// <summary>
    /// Execute the full specification suite against the reference implementation.
    /// Asserts that no specification case fails; if any fail, the names of failing cases are shown.
    /// </summary>
    [Fact]
    public void ReferenceImplementation_Should_Satisfy_All_Specs()
    {
        var (svc, repo, auth, clock) = NewWorld();

        // Build the baseline set of specification cases using the test doubles.
        var cases = SpecSuite.CreateBaselineSpecs(repo, auth, clock);

        // Execute all specification cases and collect results.
        var (passed, failed, failures) = SpecSuite.ExecuteAll(svc, cases, _out);

        // Expect zero failures: otherwise include failing case names in the assertion message.
        Assert.True(failed == 0, $"Reference failed {failed} cases: {string.Join(", ", failures.Select(f => f.name))}");
    }
}
