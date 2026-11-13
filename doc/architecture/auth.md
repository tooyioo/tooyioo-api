# Auth

Authentication is host-level infrastructure. Authorization and identity-dependent mapping are part of the slice behavior when they affect a request.

## Direction

Tooyioo delegates human authentication to an external OpenID Connect provider.

Google is the first provider because it keeps early product authentication simple and avoids storing user passwords or password-reset credentials in Tooyioo. This is an implementation choice, not a domain dependency. The intended direction is that the API can later move to another delegated OpenID Connect mechanism, such as Auth0 or a similar identity platform, without forcing most slices to understand Google-specific claims.

The API boundary should therefore distinguish:

- external identity claims, such as provider `iss` and `sub`
- the app-owned `user_id`
- app authorization facts, such as roles, scopes, organization membership, or permissions when those are introduced

Most endpoints should authorize from the app-owned `user_id` claim. They should not depend on Google-specific claims directly.

The default and fallback authorization policies require `user_id`. Slicent slice endpoints call `RequireAuthorization()` without a policy, so they use the default policy; route authorization filters then enforce ownership of the route resource. The `initiate_user_onboarding` slice is the exception: it explicitly uses the `sub-required` policy because a new user has no `user_id` claim yet.

The main exception is the `initiate_user_onboarding` slice. It must consume external identity and personal-detail claims because its purpose is to turn a trusted external identity into a Tooyioo onboarding process.

## The user login-to-user mapping

The token filter does not trust the raw `sub` as an identity. It resolves a pseudonymous user login to an app-owned `user_id`:

```
user_login_key = HMAC-SHA256(pepper, "oidc|" + issuer + "|" + subject)
```

- The stream `user_login_claiming/{user_login_key}` holds `UserLoginClaimed(user_id, user_onboarding_id)` and `UserLoginReleased(user_id)`.
- A claimed user login resolves to its `user_id`; a released or unknown user login resolves to no user.
- The raw `sub` is never stored. Stream names end up in `$all`, backups, and logs, so the key is a keyed hash, not plaintext.
- The issuer comes from the validated token and is matched exactly by the JWT middleware (`Google.Issuer`). Changing an issuer re-keys existing user logins, so it is a one-time user-login migration, not a re-architecture.
- The pepper is a single long-lived deployment value. It lives in the platform secret store (or `dotnet user-secrets` locally), must be identical across replicas, and must be backed up. Losing it or changing it means no existing user login can be resolved. It is never stored in the event store. Routine rotation is not planned; keeping the value stable and recoverable is more important to this system than rotating it on a schedule.

### Pepper exposure and rotation

The pepper is not an authentication credential. Disclosure by itself does not let someone sign in as a user or write to KurrentDB. It lets someone who knows a provider subject calculate that login's deterministic stream key. This primarily weakens the pseudonymity of login identifiers, especially if the event store or its identifiers are also exposed. Protect the event store and API credentials as the primary security boundary.

If the pepper is exposed, assess whether the event store or related identifiers were exposed as well, and prevent further disclosure of the value. A pepper-only exposure does not require an automatic emergency rotation. If rotation is chosen, plan it as a migration: the raw provider subject is deliberately not stored, so existing keys cannot be re-derived offline. The API would need a versioned key scheme and a transition period that can resolve old and new keys, then migrate a login when its user authenticates again. Update both login-claiming and user-login-linking streams safely, and retain the old pepper until migration is complete. Dormant users cannot be migrated from the stored HMAC keys alone.

The token filter reads the raw `iss` and `sub`. It does not normalize them. The JWT middleware's `ValidIssuers` is the issuer security boundary. The user login key factory uses the raw validated `iss` as the key input; it does not normalize it. The raw claim is never mutated.

The reverse index `user_login_linking/{user_id}` records the user logins linked to a user. It is used to release every user login when the person is erased, and it is the foundation for linking multiple external identities to the same `user_id`. If a future IdP federates those identities into one `sub`, the app sees one login and the index has one entry; there is no multi-login at the app level.

## Passwords, 2FA and the IdP

Tooyioo never stores passwords or second factors. Password verification, 2FA, account recovery and account linking at the provider are the IdP's responsibility. The API receives an OIDC/SAML token after the IdP has applied its policy. `kind` identifies the external mechanism (today `oidc`), not a local password. If the app needs to require 2FA for sensitive actions, it can inspect the IdP's `amr`/`acr` claims or rely on the IdP policy; it does not add a login kind or a login key.

## JWT claims

JWT claims should remain honest.

Do not mutate the identity token into something it is not. The token represents the external identity provider's claims.

For Google-style identity tokens, relevant claims include:

- `sub`
- `iss`
- `given_name`
- `family_name`
- `email`
- `email_verified`

Slices may read `ClaimsPrincipal` when the request behavior depends on authenticated identity.

## App-owned claims

The application enriches the authenticated principal with one app-owned claim after token validation:

- `user_id`: the user resolved from the claimed user login.

There is no `user_onboarding_id` claim. The user onboarding process has its own `user_onboarding_id` in the route; the authorizer loads the onboarding state and compares its `user_id` with the claim. Onboarding state (in progress, completed, canceled) is a domain fact, not a separate identity.

Business slices receive an authenticated principal with `user_id` and do not know which provider produced the token.

## Provider evolution

The current host validates Google identity tokens directly. A future provider can replace that direct Google validation as long as it produces a trusted `ClaimsPrincipal` with the `user_id` claim that Tooyioo endpoints require.

Possible future shapes include:

- an external identity platform federating to Google and issuing API tokens for Tooyioo
- a Tooyioo token exchange endpoint that verifies an external identity token and issues short-lived app tokens
- a separate machine-to-machine credential flow for non-human API clients

Because the user login key is derived from the provider and the subject, adding a provider is a configuration and link decision, not a schema change. The `user_id` never changes when a user login is added or removed.

## Personal details

If personal details come from the identity token, parse them from the claims principal at onboarding initiation and write them only to the `user_personal_data` stream.

Do not use the email address as an identity. It can change. The identity is the user login key, and the app-owned identity is the `user_id`.

Do not introduce a separate personal-details retriever unless the source is genuinely different from the token, for example a separate provider userinfo API call, a privacy transform, or a provider-specific normalization flow.

## Tests

Auth test guidance lives in [Testing](../guides/testing.md#auth-in-tests).

The test host replaces `IUserLoginKeyFactory` with a fake that uses the subject itself as the user login key. Tests seed `user_login_claiming/{subject}` and assert against the same subject, so they do not depend on the HMAC pepper.
