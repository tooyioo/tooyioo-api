# =============================================================================
# PROPUESTA - No forma parte del modelo vigente (doc/event-model).
# Superseded by the canonical model in doc/event-model; kept as a design record.
#
# Objetivo: ver de un vistazo el flujo de user onboarding, los streams en los
# que se escribe cada comando, y el ciclo de vida del borrado de datos
# personales (right to be forgotten) con soft delete / truncate.
#
# Mapa de streams (un aggregate = un stream):
#
#   personal_data/{user_id}                  PII. Único stream con datos personales.
#   login_credential_claiming/{credential_key}  credential_key -> user_id
#     credential_key = HMAC(pepper, kind|provider_id|subject)
#   user_login_credentials/{user_id}          índice inverso: credenciales del usuario
#   user_onboarding/{user_id}                proceso de onboarding (sin PII)
#   user_alias_claiming/{alias}                   alias -> user_id
#   business_onboarding/{business_onboarding_id}  datos de negocio (sin cambios)
#
# Reglas del diseño:
#   - El userId se acuña en el mapper de initiate_user_onboarding.
#   - No existe userOnboardingId: el id del onboarding es el userId.
#   - La PII solo se escribe en personal_data. Ningún otro evento la contiene.
#   - El borrado se modela con eventos en personal_data y se aplica truncando
#     el stream antes del evento PersonalDataErasureRequested, de modo que
#     sobreviven Requested/Applied y desaparece la PII.
#   - El JWT nunca dice "onboarding" ni "user": dice credencial -> user_id.
#   - provider_id es una clave estable de configuracion (google, auth0-prod),
#     no el issuer crudo. Cambiar el dominio de un IdP no re-clava credenciales;
#     cambiar de proveedor si, y se resuelve reclamando la clave nueva para el
#     mismo user_id y liberando la vieja.
# =============================================================================

board {
  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_login_credential,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.complete_user_onboarding,
    workflow.project_users,
    workflow.cancel_user_onboarding,
    workflow.retrieve_personal_data,
    workflow.request_personal_data_erasure,
    workflow.cancel_personal_data_erasure,
    workflow.apply_due_personal_data_erasures,
  ]
}

chapter "chapter_01_user_onboarding" {
  title = "Chapter 1 - User Onboarding"

  workflows = [
    workflow.initiate_user_onboarding,
    workflow.retrieve_claimed_login_credential,
    workflow.retrieve_user_onboarding,
    workflow.choose_user_onboarding_alias,
    workflow.retrieve_claimed_alias,
    workflow.complete_user_onboarding,
    workflow.project_users,
  ]
}

chapter "chapter_02_user_onboarding_cancellation" {
  title = "Chapter 2 - Alternative: User Onboarding Cancellation"

  workflows = [
    workflow.cancel_user_onboarding,
  ]
}

chapter "chapter_03_personal_data" {
  title = "Chapter 3 - Personal Data and Erasure"

  workflows = [
    workflow.retrieve_personal_data,
    workflow.request_personal_data_erasure,
    workflow.cancel_personal_data_erasure,
    workflow.apply_due_personal_data_erasures,
  ]
}

actor "authenticated_user" {
  title         = "Authenticated User"
  auth_required = true
}

actor "system" {
  title         = "System (scheduled job)"
  auth_required = false
}

# =============================================================================
# Personal data
# =============================================================================

bounded_context "personal_data" {
  title       = "Personal Data"
  description = "The only bounded context that stores personal identifiable information."

  aggregate "personal_data" {
    title       = "Personal Data"
    description = "Stream personal_data/{user_id}. Holds the only PII in the system. When an erasure is applied, the stream is truncated before PersonalDataErasureRequested so the erasure events survive and the PII disappears."
  }

  event "personal_data_recorded" {
    title       = "Personal Data Recorded"
    description = "A person's identifying data was recorded. Written only at user onboarding initiation."
    aggregate   = aggregate.personal_data

    field "name" {
      type    = "String"
      example = "Joe"
      pii     = true
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
      pii     = true
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
      pii     = true
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }
  }

  event "personal_data_erasure_requested" {
    title       = "Personal Data Erasure Requested"
    description = "The person asked to be forgotten. The data is still readable until the request is applied, so the request can be canceled during the grace period."
    aggregate   = aggregate.personal_data

    field "reason_code" {
      type    = "String"
      example = "user_request"
    }

    field "erase_after_utc" {
      type    = "DateTime"
      example = "2026-11-16T00:00:00.000Z"
    }
  }

  event "personal_data_erasure_canceled" {
    title       = "Personal Data Erasure Canceled"
    description = "The person changed their mind before the erasure was applied. The personal data was never truncated."
    aggregate   = aggregate.personal_data
  }

  event "personal_data_erasure_applied" {
    title       = "Personal Data Erasure Applied"
    description = "The erasure was executed: credentials were released and the stream was truncated before PersonalDataErasureRequested. This event survives the truncation and is what read models use to anonymize."
    aggregate   = aggregate.personal_data
  }
}

# =============================================================================
# Authentication: credentials are pseudonymous links to a user_id
# =============================================================================

# credential_key = HMAC(pepper, kind + "|" + provider_id + "|" + subject)
#
# provider_id is a stable configuration key, for example:
#   google      -> https://accounts.google.com
#   auth0-prod  -> https://tooyioo.eu.auth0.com/
#
# The issuer -> provider_id mapping is an allowlist validated at startup.
# Changing the IdP domain only changes the mapping. Changing the provider
# (google -> auth0-prod) is a migration: claim the new credential key for the
# same user_id and release the old one.

bounded_context "authentication" {
  title       = "Authentication"
  description = "Maps a login credential to a user_id. The raw credential (subject or email) is never stored; the stream name is a keyed hash. provider_id is a stable configured key, not the raw issuer."

  aggregate "login_credential_claiming" {
    title       = "Login Credential Claiming"
    description = "Stream login_credential_claiming/{credential_key}. One stream per credential. credential_key = HMAC(pepper, kind + '|' + provider_id + '|' + subject). provider_id comes from a configured issuer allowlist, so changing an IdP domain does not re-key credentials; changing the provider itself is a one-time migration. This is what the token filter reads to resolve a user_id."
  }

  aggregate "user_login_credentials" {
    title       = "User Login Credentials"
    description = "Stream user_login_credentials/{user_id}. Reverse index of the credentials linked to a user, used to release them all when the person is erased and to list linked logins."
  }

  event "login_credential_claimed" {
    title       = "Login Credential Claimed"
    description = "A login credential was claimed by a user_id. This is the only lookup the token filter needs. provider_id is the stable key used in the hash; provider is only a display name."
    aggregate   = aggregate.login_credential_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }

    field "kind" {
      type    = "String"
      example = "oidc"
    }

    field "provider_id" {
      type    = "String"
      example = "google"
    }

    field "provider" {
      type     = "String"
      optional = true
      example  = "Google"
    }
  }

  event "login_credential_released" {
    title       = "Login Credential Released"
    description = "The credential no longer belongs to a user_id and can be claimed again. This is what allows the same Google account to sign up again after an erasure."
    aggregate   = aggregate.login_credential_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "login_credential_linked" {
    title       = "Login Credential Linked"
    description = "A credential was added to a user's index. Used for multi-credential accounts (for example Google direct plus an IdP, or one IdP plus a password)."
    aggregate   = aggregate.user_login_credentials

    field "credential_key" {
      type    = "String"
      example = "5f0c2e..."
    }

    field "kind" {
      type    = "String"
      example = "oidc"
    }

    field "provider_id" {
      type    = "String"
      example = "auth0-prod"
    }

    field "provider" {
      type     = "String"
      optional = true
      example  = "Auth0"
    }
  }

  event "login_credential_unlinked" {
    title       = "Login Credential Unlinked"
    description = "A credential was removed from a user's index."
    aggregate   = aggregate.user_login_credentials

    field "credential_key" {
      type    = "String"
      example = "5f0c2e..."
    }
  }
}

# =============================================================================
# User onboarding
# =============================================================================

bounded_context "user_onboarding" {
  title       = "User Onboarding"
  description = "The onboarding process of a person. The stream id is the user_id; there is no separate user_onboarding_id."

  aggregate "user_onboarding" {
    title       = "User Onboarding"
    description = "Stream user_onboarding/{user_id}. Process state: initiated -> alias chosen -> completed, or -> canceled. It never contains PII and it is never deleted."
  }

  aggregate "user_alias_claiming" {
    title       = "User Alias Claiming"
    description = "Stream user_alias_claiming/{alias}. An alias can belong to only one user at a time. Released aliases can be claimed again."
  }

  event "user_onboarding_initiated" {
    title       = "User Onboarding Initiated"
    description = "A person started user onboarding. The user_id is the stream id; the event carries no PII."
    aggregate   = aggregate.user_onboarding
  }

  event "user_onboarding_alias_chosen" {
    title       = "User Onboarding Alias Chosen"
    description = "An alias was chosen for the user onboarding."
    aggregate   = aggregate.user_onboarding

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }

  event "user_onboarding_completed" {
    title       = "User Onboarding Completed"
    description = "The user onboarding was completed and the terms and conditions version was accepted. Consent evidence must survive an erasure, so it lives here and not in personal_data."
    aggregate   = aggregate.user_onboarding

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  event "user_onboarding_canceled" {
    title       = "User Onboarding Canceled"
    description = "The user onboarding was canceled. Only a reason code is stored; free text is not allowed because it could contain PII."
    aggregate   = aggregate.user_onboarding

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }

  event "alias_claimed" {
    title       = "Alias Claimed"
    description = "An alias was claimed by a user."
    aggregate   = aggregate.user_alias_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "alias_released" {
    title       = "Alias Released"
    description = "An alias was released by a user and can be claimed again."
    aggregate   = aggregate.user_alias_claiming

    field "user_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }
}

# =============================================================================
# Business onboarding
# Kept for context. It is out of scope for this proposal except for the rule
# that business records are not part of the erasure: tax and accounting data
# have legal retention.
# =============================================================================

bounded_context "business_onboarding" {
  title       = "Business Onboarding"
  description = "Business records. Legal name, address and tax id are business data with legal retention; they are not erased with the person."

  aggregate "business_onboarding" {
    title       = "Business Onboarding"
    description = "Stream business_onboarding/{business_onboarding_id}. initiated_by is a user_id and becomes a dangling pseudonym after an erasure."
  }

  event "business_onboarding_initiated" {
    title       = "Business Onboarding Initiated"
    description = "A business onboarding process was initiated for a user."
    aggregate   = aggregate.business_onboarding

    field "legal_name" {
      type    = "String"
      example = "Acme Ltd"
    }

    field "initiated_by" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000010"
    }
  }

  event "business_onboarding_address_recorded" {
    title       = "Business Onboarding Address Recorded"
    description = "A business address was recorded. Personal data only when the business is a sole trader."
    aggregate   = aggregate.business_onboarding

    field "line1" {
      type    = "String"
      example = "Avenida San Javier 67"
    }

    field "line2" {
      type     = "String"
      optional = true
      example  = "Portal 15, 4ºF"
    }

    field "town_name" {
      type    = "String"
      example = "Málaga"
    }

    field "post_code" {
      type    = "String"
      example = "29140"
    }

    field "country_subdivision" {
      type    = "String"
      example = "Málaga"
    }

    field "country" {
      type    = "String"
      example = "ES"
    }
  }

  event "business_onboarding_tax_details_recorded" {
    title       = "Business Onboarding Tax Details Recorded"
    description = "A tax id was recorded. Personal data when the business is a sole trader; retained for legal obligations."
    aggregate   = aggregate.business_onboarding

    field "tax_id" {
      type    = "String"
      example = "B12345678"
    }
  }

  event "business_onboarding_contact_details_recorded" {
    title       = "Business Onboarding Contact Details Recorded"
    description = "Business contact details. A role mailbox is business data; a person's mailbox or phone is personal data."
    aggregate   = aggregate.business_onboarding

    field "email" {
      type     = "String"
      optional = true
      example  = "info@acme.com"
    }

    field "phone" {
      type     = "String"
      optional = true
      example  = "+34644000000"
    }

    field "website" {
      type     = "String"
      optional = true
      example  = "https://acme.com"
    }
  }

  event "business_onboarding_completed" {
    title       = "Business Onboarding Completed"
    description = "The business onboarding was completed."
    aggregate   = aggregate.business_onboarding

    field "business_id" {
      type    = "String"
      example = "00000000-0000-0000-0000-000000000100"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }

  event "business_onboarding_canceled" {
    title       = "Business Onboarding Canceled"
    description = "The business onboarding was canceled."
    aggregate   = aggregate.business_onboarding

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }
}

# =============================================================================
# Chapter 1 - User onboarding
# =============================================================================

# Writes 4 streams in one atomic multi-stream append:
#   personal_data/{user_id}
#   user_login_credentials/{user_id}
#   login_credential_claiming/{credential_key}
#   user_onboarding/{user_id}
state_change "initiate_user_onboarding" {
  title       = "Initiate User Onboarding"
  description = "POST /user-onboarding/initiate. Resolves the login credential, mints a user_id, records the person's PII, links the credential and starts the onboarding process. A retry with an already claimed credential returns the existing user_id and appends nothing."

  command "initiate_user_onboarding" {
    title       = "Initiate User Onboarding"
    description = "Generates the user_id in the mapper, derives credential_key = HMAC(pepper, kind + '|' + provider_id + '|' + subject) and writes personal data, credential link, credential claim and onboarding initiation atomically."
    aggregate   = aggregate.user_onboarding.user_onboarding
    api_endpoint = "POST /user-onboarding/initiate"

    aggregate_dependencies = [
      aggregate.personal_data.personal_data,
      aggregate.authentication.user_login_credentials,
      aggregate.authentication.login_credential_claiming,
    ]

    to = [
      event.personal_data.personal_data_recorded,
      event.authentication.login_credential_linked,
      event.authentication.login_credential_claimed,
      event.user_onboarding.user_onboarding_initiated,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "name" {
      type    = "String"
      example = "Joe"
    }

    field "last_name" {
      type    = "String"
      example = "Bloggs"
    }

    field "email" {
      type    = "String"
      example = "joe@test.com"
    }

    field "is_email_verified" {
      type    = "Boolean"
      example = true
    }

    field "kind" {
      type    = "String"
      example = "oidc"
    }

    field "credential_provider_id" {
      type    = "String"
      example = "google"
    }

    field "credential_provider" {
      type     = "String"
      optional = true
      example  = "Google"
    }
  }
}

# Read model used by the authentication filter:
#   credential_key -> user_id (and whether it is currently claimed)
state_view "retrieve_claimed_login_credential" {
  title       = "Retrieve Claimed Login Credential"
  description = "Internal lookup; not an HTTP endpoint. The token filter derives the credential key, reads this state and adds the user_id claim. A released credential resolves to no user."

  readmodel "claimed_login_credential" {
    title       = "Claimed Login Credential"
    description = "In-memory command-side state rehydrated from the login_credential_claiming stream."
    question    = "Which user_id, if any, is this login credential claimed by?"

    from = [
      event.authentication.login_credential_claimed,
      event.authentication.login_credential_released,
    ]

    field "credential_key" {
      type         = "String"
      id_attribute = true
      example      = "5f0c2e..."
    }

    field "kind" {
      type     = "String"
      optional = true
      example  = "oidc"
    }

    field "provider_id" {
      type     = "String"
      optional = true
      example  = "google"
    }

    field "user_id" {
      type     = "String"
      optional = true
      example  = "00000000-0000-0000-0000-000000000010"
    }

    field "is_claimed" {
      type    = "Boolean"
      example = true
    }
  }
}

# Read model for GET /user-onboarding/{user_id}. It has no PII; the screen
# reads name and email from the Personal Data read model.
state_view "retrieve_user_onboarding" {
  title       = "Retrieve User Onboarding"
  description = "GET /user-onboarding/{user_id} returns the persisted User Onboarding projection. The id is the user_id; there is no separate onboarding id."

  readmodel "user_onboarding" {
    title       = "User Onboarding"
    description = "Persisted User Onboarding read model assembled from the onboarding process events. No PII."
    question    = "What is the current state of this user onboarding?"

    from = [
      event.user_onboarding.user_onboarding_initiated,
      event.user_onboarding.user_onboarding_alias_chosen,
      event.user_onboarding.user_onboarding_completed,
      event.user_onboarding.user_onboarding_canceled,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "terms_and_conditions_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }

    field "created_at" {
      type    = "DateTime"
      example = "2026-11-01T00:00:00.000Z"
    }

    field "last_modified_at" {
      type    = "DateTime"
      example = "2026-11-01T00:10:00.000Z"
    }

    field "revision" {
      type    = "Long"
      example = 3
    }
  }
}

# Writes 2 streams in one atomic multi-stream append:
#   user_onboarding/{user_id}
#   user_alias_claiming/{alias}
state_change "choose_user_onboarding_alias" {
  title       = "Choose User Onboarding Alias"
  description = "POST /user-onboarding/{user_id}/choose-alias. Claims an alias for the user. Choosing the same alias again is idempotent; choosing an alias owned by another user is rejected."

  command "choose_user_onboarding_alias" {
    title       = "Choose User Onboarding Alias"
    description = "Writes the alias on the onboarding process and claims it for the user in one atomic append."
    aggregate   = aggregate.user_onboarding.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_id}/choose-alias"

    aggregate_dependencies = [
      aggregate.user_onboarding.user_alias_claiming,
    ]

    to = [
      event.user_onboarding.user_onboarding_alias_chosen,
      event.user_onboarding.alias_claimed,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type    = "String"
      example = "joe-1984"
    }
  }
}

state_view "retrieve_claimed_alias" {
  title       = "Retrieve Claimed Alias"
  description = "Internal lookup; not an HTTP endpoint. Command-side state rehydrated from the user_alias_claiming stream."

  readmodel "claimed_alias" {
    title       = "Claimed Alias"
    description = "In-memory command-side state rehydrated from the user_alias_claiming stream."
    question    = "Which user_id, if any, currently owns this alias?"

    from = [
      event.user_onboarding.alias_claimed,
      event.user_onboarding.alias_released,
    ]

    field "alias" {
      type         = "String"
      id_attribute = true
      example      = "joe-1984"
    }

    field "user_id" {
      type     = "String"
      optional = true
      example  = "00000000-0000-0000-0000-000000000010"
    }
  }
}

# Writes 1 stream:
#   user_onboarding/{user_id}
state_change "complete_user_onboarding" {
  title       = "Complete User Onboarding"
  description = "POST /user-onboarding/{user_id}/complete. Completes an onboarding that already has an alias and records the accepted terms and conditions version. No id is minted here: the user_id already exists."

  command "complete_user_onboarding" {
    title       = "Complete User Onboarding"
    description = "Appends UserOnboardingCompleted to the process stream. Terms acceptance is consent evidence and must survive an erasure."
    aggregate   = aggregate.user_onboarding.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_id}/complete"

    to = [
      event.user_onboarding.user_onboarding_completed,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "terms_and_conditions_version" {
      type    = "String"
      example = "1.0.0"
    }
  }
}

# Read model for the user directory. It is a PII copy for search, so it is
# anonymized when PersonalDataErasureApplied is projected.
state_view "project_users" {
  title       = "Project Users"
  description = "Internal projection; not an HTTP endpoint. Projects completed user onboardings, the chosen alias and the personal data copy used by the directory search."

  readmodel "users" {
    title       = "Users"
    description = "Persisted Users read model consumed by the directory queries. Holds a copy of the PII for search and anonymizes it on PersonalDataErasureApplied."
    question    = "Which users match the requested filters?"

    from = [
      event.personal_data.personal_data_recorded,
      event.user_onboarding.user_onboarding_alias_chosen,
      event.user_onboarding.user_onboarding_completed,
      event.user_onboarding.user_onboarding_canceled,
      event.personal_data.personal_data_erasure_applied,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "alias" {
      type     = "String"
      optional = true
      example  = "joe-1984"
    }

    field "name" {
      type     = "String"
      optional = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      example  = "joe@test.com"
    }

    field "status" {
      type    = "String"
      example = "completed"
    }

    field "terms_and_conditions_version" {
      type     = "String"
      optional = true
      example  = "1.0.0"
    }
  }
}

# =============================================================================
# Chapter 2 - Cancellation
# =============================================================================

# Writes 4 streams in one atomic multi-stream append:
#   user_onboarding/{user_id}          UserOnboardingCanceled
#   login_credential_claiming/{key}     LoginCredentialReleased
#   user_alias_claiming/{alias}             AliasReleased            (only if an alias exists)
#   personal_data/{user_id}            PersonalDataErasureRequested (erase_after = now)
#
# A canceled onboarding is erased immediately: the erasure request is part of
# the same atomic append, so there is no onboarding without a pending erasure.
state_change "cancel_user_onboarding" {
  title       = "Cancel User Onboarding"
  description = "POST /user-onboarding/{user_id}/cancel. Cancels an in-progress onboarding, releases its login credential and alias, and requests an immediate personal data erasure."

  command "cancel_user_onboarding" {
    title       = "Cancel User Onboarding"
    description = "One atomic append for the cancellation, the two releases and the erasure request. The erasure automation then applies it."
    aggregate   = aggregate.user_onboarding.user_onboarding
    api_endpoint = "POST /user-onboarding/{user_id}/cancel"

    aggregate_dependencies = [
      aggregate.authentication.login_credential_claiming,
      aggregate.user_onboarding.user_alias_claiming,
      aggregate.personal_data.personal_data,
    ]

    to = [
      event.user_onboarding.user_onboarding_canceled,
      event.authentication.login_credential_released,
      event.user_onboarding.alias_released,
      event.personal_data.personal_data_erasure_requested,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "reason_code" {
      type    = "String"
      example = "changed_mind"
    }
  }
}

# =============================================================================
# Chapter 3 - Personal data and erasure
# =============================================================================

# Read model for GET /personal-data/{user_id}. It holds the PII copy shown to
# the owner and is anonymized when the erasure is applied.
state_view "retrieve_personal_data" {
  title       = "Retrieve Personal Data"
  description = "GET /personal-data/{user_id} returns the person's identifying data and the erasure status. The caller must be the same user or an operator."

  readmodel "personal_data" {
    title       = "Personal Data"
    description = "Persisted Personal Data read model. The only read model whose purpose is to expose PII to its owner. Anonymized on PersonalDataErasureApplied."
    question    = "Which personal data and erasure status does this user have?"

    from = [
      event.personal_data.personal_data_recorded,
      event.personal_data.personal_data_erasure_requested,
      event.personal_data.personal_data_erasure_canceled,
      event.personal_data.personal_data_erasure_applied,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "name" {
      type     = "String"
      optional = true
      example  = "Joe"
    }

    field "last_name" {
      type     = "String"
      optional = true
      example  = "Bloggs"
    }

    field "email" {
      type     = "String"
      optional = true
      example  = "joe@test.com"
    }

    field "status" {
      type    = "String"
      example = "active"
    }

    field "erase_after_utc" {
      type     = "DateTime"
      optional = true
      example  = "2026-11-16T00:00:00.000Z"
    }
  }
}

# Writes 1 stream:
#   personal_data/{user_id}          PersonalDataErasureRequested (erase_after = now + grace)
state_change "request_personal_data_erasure" {
  title       = "Request Personal Data Erasure"
  description = "DELETE /personal-data/{user_id} (right to be forgotten). Starts the grace period. The data remains readable and the request can be canceled until the grace period ends."

  command "request_personal_data_erasure" {
    title       = "Request Personal Data Erasure"
    description = "Appends PersonalDataErasureRequested with the due date computed from the configured grace period."
    aggregate   = aggregate.personal_data.personal_data
    api_endpoint = "DELETE /personal-data/{user_id}"

    to = [
      event.personal_data.personal_data_erasure_requested,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }

    field "reason_code" {
      type    = "String"
      example = "user_request"
    }

    field "erase_after_utc" {
      type    = "DateTime"
      example = "2026-11-16T00:00:00.000Z"
    }
  }
}

# Writes 1 stream:
#   personal_data/{user_id}          PersonalDataErasureCanceled
state_change "cancel_personal_data_erasure" {
  title       = "Cancel Personal Data Erasure"
  description = "POST /personal-data/{user_id}/cancel-erasure. Cancels a pending erasure during the grace period. Rejected once the erasure has been applied."

  command "cancel_personal_data_erasure" {
    title       = "Cancel Personal Data Erasure"
    description = "Appends PersonalDataErasureCanceled. The personal data was never truncated."
    aggregate   = aggregate.personal_data.personal_data
    api_endpoint = "POST /personal-data/{user_id}/cancel-erasure"

    to = [
      event.personal_data.personal_data_erasure_canceled,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }
  }
}

# Applies erasures whose erase_after_utc has passed.
#
# Writes 3 streams in one atomic multi-stream append:
#   personal_data/{user_id}            PersonalDataErasureApplied
#   login_credential_claiming/{key}     LoginCredentialReleased
#   user_login_credentials/{user_id}    LoginCredentialUnlinked
#
# Then truncates personal_data/{user_id} before the
# PersonalDataErasureRequested event. The stream keeps
# [PersonalDataErasureRequested, PersonalDataErasureApplied] and loses the PII.
#
# Order matters: Applied is appended before the truncate. If the truncate fails,
# the erasure state and the anonymized read models are already correct and the
# job retries the truncate.
automation "apply_due_personal_data_erasures" {
  title       = "Apply Due Personal Data Erasures"
  description = "Scheduled job. Every minute it queries the personal data read model for requests whose erase_after_utc has passed and applies them idempotently."

  command "apply_personal_data_erasure" {
    title       = "Apply Personal Data Erasure"
    description = "Appends PersonalDataErasureApplied and releases every credential of the user, then truncates the Personal Data stream before the erasure request."
    aggregate   = aggregate.personal_data.personal_data
    external_trigger = true

    aggregate_dependencies = [
      aggregate.authentication.login_credential_claiming,
      aggregate.authentication.user_login_credentials,
    ]

    to = [
      event.personal_data.personal_data_erasure_applied,
      event.authentication.login_credential_released,
      event.authentication.login_credential_unlinked,
    ]

    field "user_id" {
      type         = "String"
      id_attribute = true
      example      = "00000000-0000-0000-0000-000000000010"
    }
  }

  processor "erasure_scheduler" {
    title       = "Erasure scheduler"
    description = "Wakes up periodically and invokes the apply command for each due request."

    from = [
      event.personal_data.personal_data_erasure_requested,
    ]

    to = [
      command.apply_personal_data_erasure,
    ]
  }
}
