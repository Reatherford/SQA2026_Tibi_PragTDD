using Bank4Us.AccountOpening;
using Xunit;

namespace Bank4Us.AccountOpening.Tests;

public class CitizenshipEligibility_RedTests
{
    [Theory]
    [InlineData(CitizenshipStatus.Citizen, ApplicationStatus.Approved)]
    [InlineData(CitizenshipStatus.PermanentResident, ApplicationStatus.NeedsExtraVerification)]
    [InlineData(CitizenshipStatus.Unknown, ApplicationStatus.Incomplete)]
    public void Citizenship_partitions_map_to_expected_next_state(CitizenshipStatus status, ApplicationStatus expected)
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with { CitizenshipStatus = status };
        var workflow = new AccountOpeningWorkflow(new StubResidencyService(_ => true), new FakeCoreBankingSystem());

        // Act
        var outcome = workflow.EvaluateCitizenship(applicant);

        // Assert
        Assert.Equal(expected, outcome);
    }
}
