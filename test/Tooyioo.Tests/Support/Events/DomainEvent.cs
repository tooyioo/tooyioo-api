using Tooyioo.Business.Contracts;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.User;
using Tooyioo.User.Contracts;

namespace Tooyioo.Tests.Support.Events;

public static class DomainEvent
{
    public static UserOnboardingInitiatedBuilder UserOnboardingInitiated()
        => new();

    public static UserOnboardingAliasChosenBuilder UserOnboardingAliasChosen()
        => new();

    public static UserOnboardingCompletedBuilder UserOnboardingCompleted()
        => new();

    public static UserOnboardingCanceledBuilder UserOnboardingCanceled()
        => new();

    public static UserLoginClaimedBuilder UserLoginClaimed()
        => new();

    public static UserLoginReleasedBuilder UserLoginReleased()
        => new();

    public static UserLoginLinkedBuilder UserLoginLinked()
        => new();

    public static UserLoginUnlinkedBuilder UserLoginUnlinked()
        => new();

    public static UserPersonalDataRecordedBuilder UserPersonalDataRecorded()
        => new();

    public static UserPersonalDataErasureRequestedBuilder UserPersonalDataErasureRequested()
        => new();

    public static UserPersonalDataErasureCanceledBuilder UserPersonalDataErasureCanceled()
        => new();

    public static UserPersonalDataErasureAppliedBuilder UserPersonalDataErasureApplied()
        => new();

    public static UserAliasClaimedBuilder UserAliasClaimed()
        => new();

    public static UserAliasReleasedBuilder UserAliasReleased()
        => new();

    public static BusinessOnboardingInitiatedBuilder BusinessOnboardingInitiated()
        => new();
}

public sealed class UserOnboardingInitiatedBuilder
{
    private string _name = "Jane";
    private string _lastName = "Bloggs";
    private string _email = "jane.bloggs@test.com";
    private bool _isEmailVerified;

    public UserOnboardingInitiatedBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserOnboardingInitiatedBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserOnboardingInitiatedBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserOnboardingInitiatedBuilder WithIsEmailVerified(bool isEmailVerified)
    {
        _isEmailVerified = isEmailVerified;
        return this;
    }

    public UserOnboardingInitiatedBuilder WithExternalIdentityId(string externalIdentityId)
        => this;

    public UserOnboardingInitiatedBuilder WithExternalIdentityIssuer(string externalIdentityIssuer)
        => this;

    public UserOnboardingDomainEvents.V1.UserOnboardingInitiated Build()
        => new();

    public UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded BuildUserPersonalData()
        => new(_name, _lastName, _email, _isEmailVerified);
}

public sealed class UserOnboardingAliasChosenBuilder
{
    private string _alias = "jane-bloggs";

    public UserOnboardingAliasChosenBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen Build()
        => new(_alias);
}

public sealed class UserOnboardingCompletedBuilder
{
    private string _termsOfServiceVersion = "v1";
    private string _privacyPolicyVersion = "v1";

    public UserOnboardingCompletedBuilder WithUserId(string userId) => this;

    public UserOnboardingCompletedBuilder WithTermsOfServiceVersion(string termsOfServiceVersion)
    {
        _termsOfServiceVersion = termsOfServiceVersion;
        return this;
    }

    public UserOnboardingCompletedBuilder WithPrivacyPolicyVersion(string privacyPolicyVersion)
    {
        _privacyPolicyVersion = privacyPolicyVersion;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingCompleted Build()
        => new(_termsOfServiceVersion, _privacyPolicyVersion);
}

public sealed class UserOnboardingCanceledBuilder
{
    private string _reasonCode = "Terms and Conditions rejected";

    public UserOnboardingCanceledBuilder WithReason(string reason)
    {
        _reasonCode = reason;
        return this;
    }

    public UserOnboardingCanceledBuilder WithReasonCode(string reasonCode)
    {
        _reasonCode = reasonCode;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingCanceled Build()
        => new(_reasonCode);
}

public sealed class UserLoginClaimedBuilder
{
    private string _userId = 1.ToGuid().ToString();

    public UserLoginClaimedBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserLoginClaimedBuilder WithUserId(UserId userId)
    {
        _userId = userId.Value;
        return this;
    }

    public UserLoginClaimingDomainEvents.V1.UserLoginClaimed Build()
        => new(_userId);
}

public sealed class UserLoginReleasedBuilder
{
    private string _userId = 1.ToGuid().ToString();

    public UserLoginReleasedBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserLoginReleasedBuilder WithUserId(UserId userId)
    {
        _userId = userId.Value;
        return this;
    }

    public UserLoginClaimingDomainEvents.V1.UserLoginReleased Build()
        => new(_userId);
}

public sealed class UserLoginLinkedBuilder
{
    private string _userLoginKey = "google-sub-123";

    public UserLoginLinkedBuilder WithUserLoginKey(string userLoginKey)
    {
        _userLoginKey = userLoginKey;
        return this;
    }

    public UserLoginLinkingDomainEvents.V1.UserLoginLinked Build()
        => new(_userLoginKey);
}

public sealed class UserLoginUnlinkedBuilder
{
    private string _userLoginKey = "google-sub-123";

    public UserLoginUnlinkedBuilder WithUserLoginKey(string userLoginKey)
    {
        _userLoginKey = userLoginKey;
        return this;
    }

    public UserLoginLinkingDomainEvents.V1.UserLoginUnlinked Build()
        => new(_userLoginKey);
}

public sealed class UserPersonalDataRecordedBuilder
{
    private string _name = "Jane";
    private string _lastName = "Bloggs";
    private string _email = "jane.bloggs@test.com";
    private bool _isEmailVerified;

    public UserPersonalDataRecordedBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserPersonalDataRecordedBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserPersonalDataRecordedBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserPersonalDataRecordedBuilder WithIsEmailVerified(bool isEmailVerified)
    {
        _isEmailVerified = isEmailVerified;
        return this;
    }

    public UserPersonalDataRecordedBuilder WithExternalIdentityId(string externalIdentityId)
        => this;

    public UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded Build()
        => new(_name, _lastName, _email, _isEmailVerified);
}

public sealed class UserPersonalDataErasureRequestedBuilder
{
    private string _reasonCode = "user_request";
    private DateTime _eraseAfterUtc = DateTime.UtcNow.AddDays(-1);

    public UserPersonalDataErasureRequestedBuilder WithReasonCode(string reasonCode)
    {
        _reasonCode = reasonCode;
        return this;
    }

    public UserPersonalDataErasureRequestedBuilder WithEraseAfterUtc(DateTime eraseAfterUtc)
    {
        _eraseAfterUtc = eraseAfterUtc;
        return this;
    }

    public UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested Build()
        => new(_reasonCode, _eraseAfterUtc);
}

public sealed class UserPersonalDataErasureCanceledBuilder
{
    public UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled Build()
        => new();
}

public sealed class UserPersonalDataErasureAppliedBuilder
{
    public UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied Build()
        => new();
}

public sealed class UserAliasClaimedBuilder
{
    private string _userId = 1.ToGuid().ToString();

    public UserAliasClaimedBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserAliasClaimedBuilder WithUserId(UserId userId)
    {
        _userId = userId.Value;
        return this;
    }

    public UserAliasClaimingDomainEvents.V1.UserAliasClaimed Build()
        => new(_userId);
}

public sealed class UserAliasReleasedBuilder
{
    private string _userId = 1.ToGuid().ToString();

    public UserAliasReleasedBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserAliasReleasedBuilder WithUserId(UserId userId)
    {
        _userId = userId.Value;
        return this;
    }

    public UserAliasClaimingDomainEvents.V1.UserAliasReleased Build()
        => new(_userId);
}

public sealed class BusinessOnboardingInitiatedBuilder
{
    private string _legalName = "Acme Ltd";
    private string _initiatedBy = 1.ToGuid().ToString();

    public BusinessOnboardingInitiatedBuilder WithLegalName(string legalName)
    {
        _legalName = legalName;
        return this;
    }

    public BusinessOnboardingInitiatedBuilder WithInitiatedBy(UserId initiatedBy)
    {
        _initiatedBy = initiatedBy;
        return this;
    }

    public BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated Build()
        => new(_legalName, _initiatedBy);
}
