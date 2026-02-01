using System;
using Bank4Us.AccountOpening;

namespace Bank4Us.AccountOpening.Tests;

internal sealed class FakeCoreBankingSystem : ICoreBankingSystem
{
    public int CreateAccountCalls { get; private set; }
    public void CreateAccount(Applicant applicant) => CreateAccountCalls++;
}

internal sealed class StubResidencyService : IResidencyVerificationService
{
    private readonly Func<Applicant, bool> _behavior;
    public StubResidencyService(Func<Applicant, bool> behavior) => _behavior = behavior;
    public bool Verify(Applicant applicant) => _behavior(applicant);
}
