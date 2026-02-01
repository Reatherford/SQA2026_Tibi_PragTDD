using Bank4Us.AccountOpening;
using Xunit;

namespace Bank4Us.AccountOpening.Tests;

public class DepositMinimumRule_RedTests
{
    public static IEnumerable<object?[]> DepositCases => new[]
    {
        new object?[] { 199m, ApplicationStatus.Cancelled }, // OFF
        new object?[] { 200m, ApplicationStatus.Approved },  // ON
        new object?[] { 201m, ApplicationStatus.Approved },  // IN
        new object?[] { null, ApplicationStatus.Incomplete } // missing
    };

    [Theory]
    [MemberData(nameof(DepositCases))]
    public void Deposit_boundary_and_missing_values_drive_status(decimal? deposit, ApplicationStatus expected)
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with { OpeningDepositAmount = deposit };
        var core = new FakeCoreBankingSystem();
        var residency = new StubResidencyService(_ => true);
        var workflow = new AccountOpeningWorkflow(residency, core);

        // Act
        var result = workflow.Process(applicant);

        // Assert
        Assert.Equal(expected, result.Status);
    }
}
