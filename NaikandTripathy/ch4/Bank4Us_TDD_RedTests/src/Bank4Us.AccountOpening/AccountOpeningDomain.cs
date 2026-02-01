using System;
using System.Collections.Generic;

namespace Bank4Us.AccountOpening;

public enum ApplicationStatus
{
    Approved,
    Cancelled,
    Incomplete,
    PendingVerification,
    NeedsExtraVerification
}

public enum IdentifierType
{
    SSN,
    ITIN,
    Passport
}

public enum CitizenshipStatus
{
    Citizen,
    PermanentResident,
    Unknown
}

public sealed record Address(string Street, string City, string State, string PostalCode);

public sealed record Applicant
{
    public IdentifierType IdentifierType { get; init; } = IdentifierType.SSN;
    public string? IdentificationNumber { get; init; }
    public Address? Address { get; init; }
    public CitizenshipStatus CitizenshipStatus { get; init; } = CitizenshipStatus.Unknown;
    public decimal? OpeningDepositAmount { get; init; }
    public bool HasResidencyDocument { get; init; }
}

public sealed record ValidationError(string Message);

public sealed record ProcessResult(ApplicationStatus Status, IReadOnlyList<ValidationError> Errors);

public interface IResidencyVerificationService
{
    bool Verify(Applicant applicant);
}

public interface ICoreBankingSystem
{
    void CreateAccount(Applicant applicant);
}

public sealed class AccountOpeningWorkflow
{
    public const decimal MinimumDeposit = 200m;

    public AccountOpeningWorkflow(IResidencyVerificationService residencyService, ICoreBankingSystem coreBanking)
    {
        ResidencyService = residencyService;
        CoreBanking = coreBanking;
    }

    public IResidencyVerificationService ResidencyService { get; }
    public ICoreBankingSystem CoreBanking { get; }

    public ProcessResult Process(Applicant applicant)
        => throw new NotImplementedException("Implement in Green phase");

    public IReadOnlyList<ValidationError> ValidateIdentificationNumber(Applicant applicant)
        => throw new NotImplementedException("Implement in Green phase");

    public ProcessResult ValidateAddress(Applicant applicant)
        => throw new NotImplementedException("Implement in Green phase");

    public ApplicationStatus EvaluateCitizenship(Applicant applicant)
        => throw new NotImplementedException("Implement in Green phase");
}

public static class ApplicantFactory
{
    public static Applicant CreateValid() => new()
    {
        IdentifierType = IdentifierType.SSN,
        IdentificationNumber = "123-45-6789",
        Address = new Address("123 Main St", "Milwaukee", "WI", "53202"),
        CitizenshipStatus = CitizenshipStatus.Citizen,
        OpeningDepositAmount = 200m,
        HasResidencyDocument = true
    };
}
