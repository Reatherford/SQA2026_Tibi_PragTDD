using Bank4Us.AccountOpening;
using Xunit;

namespace Bank4Us.AccountOpening.Tests;

public class IdentificationNumber_RedTests
{
    public static IEnumerable<object?[]> InvalidSsns => new[]
    {
        new object?[] { "123" },
        new object?[] { "123-45-678" },
        new object?[] { "ABC-DE-FGHI" },
    };

    [Theory]
    [MemberData(nameof(InvalidSsns))]
    public void Invalid_ssn_format_returns_specific_validation_error(string ssn)
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with { IdentifierType = IdentifierType.SSN, IdentificationNumber = ssn };
        var workflow = new AccountOpeningWorkflow(new StubResidencyService(_ => true), new FakeCoreBankingSystem());

        // Act
        var errors = workflow.ValidateIdentificationNumber(applicant);

        // Assert
        Assert.Contains(errors, e => e.Message == "Invalid SSN format");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Missing_id_number_blocks_processing_and_cancels_application(string? id)
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with { IdentificationNumber = id };
        var workflow = new AccountOpeningWorkflow(new StubResidencyService(_ => true), new FakeCoreBankingSystem());

        // Act
        var result = workflow.Process(applicant);

        // Assert
        Assert.Equal(ApplicationStatus.Cancelled, result.Status);
        Assert.Contains(result.Errors, e => e.Message.Contains("ID", StringComparison.OrdinalIgnoreCase));
    }
}
