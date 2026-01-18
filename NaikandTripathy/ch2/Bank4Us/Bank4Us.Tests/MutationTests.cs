using System.Collections.Generic;
using System.Linq;
using Bank4Us.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Bank4Us.Tests;

/// <summary>
/// Tests that exercise mutation testing against the baseline specification suite.
/// Each mutant is represented by an <see cref="IBankRuleSet"/> implementation that intentionally
/// contains a fault. The test runs the spec suite against each mutant and asserts that all mutants
/// are killed (i.e., at least one spec fails for each mutant).
/// </summary>
public sealed class MutationTests
{
    private readonly ITestOutputHelper _out;

    /// <summary>
    /// Test class constructor - captures the xUnit test output helper for logging.
    /// </summary>
    /// <param name="output">xUnit provided output helper.</param>
    public MutationTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// Create a new test world with the supplied rule set.
    /// Returns the service under test and the supporting test doubles:
    /// - repository seeded with two accounts,
    /// - authorizer,
    /// - clock.
    /// </summary>
    /// <param name="rules">The rule set (possibly mutated) to drive service behavior.</param>
    /// <returns>Tuple of service, repository, authorizer, and clock.</returns>
    private static (BankAccountService svc, IAccountRepository repo, IAuthorizer auth, IClock clock)
        NewWorldWithRules(IBankRuleSet rules)
    {
        // Create repository and test doubles seeded with two accounts:
        // - "C1": personal checking
        // - "S1": savings
        var (repo, auth, clock) = TestDoubles.CreateRepoWith(
            new Account("C1", AccountType.PersonalChecking, 0m),
            new Account("S1", AccountType.Savings, 0m)
        );

        // Construct the service using the supplied rule set so each mutant can be exercised.
        var svc = new BankAccountService(repo, auth, clock, rules);
        return (svc, repo, auth, clock);
    }

    /// <summary>
    /// Enumerates the faulty rule sets (mutants) to be tested.
    /// Each yielded tuple contains a short identifier name and the mutant instance.
    /// Add new mutants here if additional faults are introduced for testing.
    /// </summary>
    /// <returns>Sequence of named mutant rule sets.</returns>
    public static IEnumerable<(string name, IBankRuleSet rules)> Mutants()
    {
        yield return ("Mutant_OffByOne_Limit", new Mutant_OffByOne_Limit());
        //yield return ("Mutant_MissingFrozenCheck", new Mutant_MissingFrozenCheck());
        //yield return ("Mutant_AllowsZeroAmount", new Mutant_AllowsZeroAmount());
        //yield return ("Mutant_OrInsteadOfAnd", new Mutant_OrInsteadOfAnd());
    }

    /// <summary>
    /// Runs the baseline specification suite against each mutant.
    /// The test is successful only if every mutant is killed (i.e., at least one spec fails).
    /// Detailed results are written to the test output for debugging surviving mutants.
    /// </summary>
    [Fact]
    public void SpecSuite_Should_Kill_All_Faulty_Mutants()
    {
        var mutants = Mutants().ToList();
        var survivors = new List<string>();

        foreach (var (name, rules) in mutants)
        {
            // Arrange: create the service and test doubles for this mutant
            var (svc, repo, auth, clock) = NewWorldWithRules(rules);

            // Build baseline specification cases using the same repository/authorizer/clock
            var cases = SpecSuite.CreateBaselineSpecs(repo, auth, clock);

            // Act: execute all specs and collect pass/fail counts and failure details
            var (passed, failed, failures) = SpecSuite.ExecuteAll(svc, cases, _out);

            // A mutant is considered killed if any spec fails against it.
            bool killed = failed > 0;

            // Log outcome for visibility in test results
            _out.WriteLine($"Mutant: {name} → {(killed ? "KILLED" : "SURVIVED")}");
            if (!killed) survivors.Add(name);
        }

        // If any mutants survived, log their names for debugging information
        if (survivors.Count > 0)
            _out.WriteLine("Survivors: " + string.Join(", ", survivors));

        // Assert that no mutants survived; if some did, prompt to add or sharpen tests.
        Assert.True(survivors.Count == 0, "Some mutants survived—add or sharpen test cases.");
    }
}
