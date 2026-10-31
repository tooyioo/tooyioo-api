using Tooyioo.BusinessOnboarding.Contracts;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Contracts;

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

    public static ExternalIdentityClaimedBuilder ExternalIdentityClaimed()
        => new();

    public static ExternalIdentityReleasedBuilder ExternalIdentityReleased()
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
    private bool _isEmailVerified = false;
    private string _externalIdentityId = "google-sub-123";
    private string _externalIdentityProvider = nameof(ExternalIdentityProvider.Google);
    private string _externalIdentityIssuer = GoogleIdentityTokenBuilder.Issuer;

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
    {
        _externalIdentityId = externalIdentityId;
        return this;
    }
    
    public UserOnboardingInitiatedBuilder WithExternalIdentityProvider(string externalIdentityProvider)
    {
        _externalIdentityProvider = externalIdentityProvider;
        return this;
    }
    
    public UserOnboardingInitiatedBuilder WithExternalIdentityIssuer(string externalIdentityIssuer)
    {
        _externalIdentityIssuer = externalIdentityIssuer;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingInitiated Build()
        => new(
            _name, 
            _lastName, 
            _email, 
            _isEmailVerified, 
            _externalIdentityId, 
            _externalIdentityProvider,
            _externalIdentityIssuer);
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
    private string _userId = 1.ToGuid().ToString();
    private string _termsAndConditionsVersion = "v1";

    public UserOnboardingCompletedBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserOnboardingCompletedBuilder WithTermsAndConditionsVersion(string termsAndConditionsVersion)
    {
        _termsAndConditionsVersion = termsAndConditionsVersion;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingCompleted Build()
        => new(_userId, _termsAndConditionsVersion);
}

public sealed class UserOnboardingCanceledBuilder
{
    private string _reason = "Terms and Conditions rejected";

    public UserOnboardingCanceledBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public UserOnboardingDomainEvents.V1.UserOnboardingCanceled Build()
        => new(_reason);
}

public sealed class ExternalIdentityClaimedBuilder
{
    private string _userOnboardingId = 1.ToGuid().ToString();

    public ExternalIdentityClaimedBuilder WithUserOnboardingId(string userOnboardingId)
    {
        _userOnboardingId = userOnboardingId;
        return this;
    }

    public ExternalIdentityClaimedBuilder WithUserOnboardingId(UserOnboardingId userOnboardingId)
    {
        _userOnboardingId = userOnboardingId.Value;
        return this;
    }

    public ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityClaimed Build()
        => new(_userOnboardingId);
}

public sealed class ExternalIdentityReleasedBuilder
{
    private string _userOnboardingId = 1.ToGuid().ToString();

    public ExternalIdentityReleasedBuilder WithUserOnboardingId(string userOnboardingId)
    {
        _userOnboardingId = userOnboardingId;
        return this;
    }

    public ExternalIdentityReleasedBuilder WithUserOnboardingId(UserOnboardingId userOnboardingId)
    {
        _userOnboardingId = userOnboardingId.Value;
        return this;
    }

    public ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityReleased Build()
        => new(_userOnboardingId);
}

public sealed class UserAliasClaimedBuilder
{
    private string _userOnboardingId = 1.ToGuid().ToString();

    public UserAliasClaimedBuilder WithUserOnboardingId(string userOnboardingId)
    {
        _userOnboardingId = userOnboardingId;
        return this;
    }

    public UserAliasClaimedBuilder WithUserOnboardingId(UserOnboardingId userOnboardingId)
    {
        _userOnboardingId = userOnboardingId.Value;
        return this;
    }

    public UserAliasClaimingDomainEvents.V1.UserAliasClaimed Build()
        => new(_userOnboardingId);
}

public sealed class UserAliasReleasedBuilder
{
    private string _userOnboardingId = 1.ToGuid().ToString();

    public UserAliasReleasedBuilder WithUserOnboardingId(string userOnboardingId)
    {
        _userOnboardingId = userOnboardingId;
        return this;
    }

    public UserAliasReleasedBuilder WithUserOnboardingId(UserOnboardingId userOnboardingId)
    {
        _userOnboardingId = userOnboardingId.Value;
        return this;
    }

    public UserAliasClaimingDomainEvents.V1.UserAliasReleased Build()
        => new(_userOnboardingId);
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
