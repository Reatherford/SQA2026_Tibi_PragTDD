using Bank4Us.AccountOpening;
using Xunit;

namespace Bank4Us.AccountOpening.Tests;

public class AddressCompleteness_RedTests
{
    [Fact]
    public void Missing_postal_code_marks_application_incomplete()
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with
        {
            Address = new Address("123 Main St", "Milwaukee", "WI", PostalCode: "")
        };
        var workflow = new AccountOpeningWorkflow(new StubResidencyService(_ => true), new FakeCoreBankingSystem());

        // Act
        var result = workflow.ValidateAddress(applicant);

        // Assert
        Assert.Equal(ApplicationStatus.Incomplete, result.Status);
        Assert.Contains(result.Errors, e => e.Message.Contains("Postal", StringComparison.OrdinalIgnoreCase));
    }
}
