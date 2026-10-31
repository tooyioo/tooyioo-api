# Auth

Authentication is host-level infrastructure. Authorization and identity-dependent mapping are part of the slice behavior when they affect a request.

## Direction

Tooyioo delegates human authentication to an external OpenID Connect provider.

Google is the first provider because it keeps early product authentication simple and avoids storing user passwords or password-reset credentials in Tooyioo. This is an implementation choice, not a domain dependency. The intended direction is that the API can later move to another delegated OpenID Connect mechanism, such as Auth0 or a similar identity platform, without forcing most slices to understand Google-specific claims.

The API boundary should therefore distinguish:

- external identity claims, such as provider `iss` and `sub`
- app-owned identity claims, such as `user_onboarding_id` and `user_id`
- app authorization facts, such as roles, scopes, organization membership, or permissions when those are introduced

Most endpoints should authorize from app-owned claims on the authenticated principal. They should not depend on Google-specific claims directly.

The main exception is the `initiate_user_onboarding` slice. It must consume external identity and personal-detail claims because its purpose is to turn a trusted external identity into a Tooyioo onboarding process.

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

The application may enrich the authenticated principal with app-owned claims after token validation.

Examples:

- known onboarding status
- known user id
- known user onboarding id

These claims are Tooyioo facts, not Google facts. Keep that distinction clear.

After onboarding has started or completed, slice authorization should prefer these app-owned claims:

- use `user_onboarding_id` for onboarding-only workflows
- use `user_id` for fully onboarded user workflows

Completed users may carry both claims. This allows idempotent onboarding endpoints to authorize retries by `user_onboarding_id` while fully onboarded workflows authorize by `user_id`.

This keeps the application authorization model stable if the upstream identity provider changes.

## Provider evolution

The current host validates Google identity tokens directly. A future provider can replace that direct Google validation as long as it produces a trusted `ClaimsPrincipal` with the app-owned claims that Tooyioo endpoints require.

Possible future shapes include:

- an external identity platform federating to Google and issuing API tokens for Tooyioo
- a Tooyioo token exchange endpoint that verifies an external identity token and issues short-lived app tokens
- a separate machine-to-machine credential flow for non-human API clients

Provider-specific parsing should stay near authentication infrastructure or the onboarding initiation boundary. Business slices should receive an already authenticated principal with Tooyioo claims.

## Personal details

If personal details come from the identity token, parse them from the claims principal.

Do not introduce a separate personal-details retriever unless the source is genuinely different from the token, for example a separate provider userinfo API call, a privacy transform, or a provider-specific normalization flow.

## Tests

Auth test guidance lives in [Testing](../guides/testing.md#auth-in-tests).
