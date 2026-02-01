using System;
using Bank4Us.AccountOpening;
using Xunit;

namespace Bank4Us.AccountOpening.Tests;

public class ResidencyVerification_RedTests
{
    [Fact]
    public void Service_unavailable_sets_pending_verification_and_does_not_create_account()
    {
        // Arrange
        var applicant = ApplicantFactory.CreateValid() with { HasResidencyDocument = true };
        var core = new FakeCoreBankingSystem();
        var residency = new StubResidencyService(_ => throw new TimeoutException("service unavailable"));
        var workflow = new AccountOpeningWorkflow(residency, core);

        // Act
        var result = workflow.Process(applicant);

        // Assert
        Assert.Equal(ApplicationStatus.PendingVerification, result.Status);
        Assert.Equal(0, core.CreateAccountCalls);
    }
}
