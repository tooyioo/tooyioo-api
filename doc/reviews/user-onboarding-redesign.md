# Revisión del rediseño: user onboarding, credenciales y erasure

Este documento es un plan de revisión del working tree actual. No es documentación canónica del sistema: la fuente de verdad sigue siendo `doc/event-model` y los tests.

## Resumen ejecutivo

Si solo tienes 20 minutos, revisa estos seis puntos en este orden:

1. **Contratos**: PII solo en `UserPersonalDataDomainEvents`; `UserOnboardingCompleted` solo lleva terms; `UserOnboardingCanceled` lleva `ReasonCode`.
2. **Auth**: el único claim de negocio es `user_id`; la credencial se resuelve por HMAC; el issuer se valida en el JWT y el parser no lo toca; no queda `user_onboarding_id`.
3. **Initiate**: los cuatro streams se escriben atómicamente; se acuñan `userOnboardingId` y `userId`; el claim guarda ambos para la idempotencia.
4. **Cancel/erasure**: la cancelación pide el erasure inmediato, libera credenciales/alias y el apply trunca la PII.
5. **Pepper único**: sin versionado ni rotación; cambiarlo invalida las credenciales existentes. Es una decisión tomada, no un pendiente.
6. **Truncate**: el apply trunca antes del request; el fake in-memory tiene off-by-one. No confundas el test con la semántica de KurrentDB.

## 0. Cómo usar este documento

- El inventario está sacado de `git status --short`, no de memoria. Incluye ficheros modificados, nuevos y borrados.
- Para ver un fichero concreto:

  ```bash
  git diff -- <ruta>
  ```

- Los ficheros nuevos no salen en `git diff` porque no están trackeados. Dos opciones:

  ```bash
  # Opción A: marcar intención de añadir, revisar, y desmarcar
  git add -N .
  git diff -- <ruta-nueva>
  git reset

  # Opción B: ver un fichero nuevo como diff contra vacío
  git diff --no-index /dev/null <ruta-nueva>
  ```

- Para cambios mecánicos de renombrado, usa `git diff -w` o `git diff --word-diff`.
- No revises el HTML generado (`doc/proposals/*.html`, `doc/tooyioo.em.html`) línea a línea. Es salida de `emhcl diagram`.

## 1. Inventario

| Tipo | Cantidad |
|---|---:|
| Ficheros modificados (tracked) | 120 |
| Ficheros nuevos (untracked) | 95 de producto (+ este documento de revisión) |
| Ficheros borrados | 16 |
| Entradas en `git status` | 185 |

Los 95 nuevos de producto incluyen 4 tests de `UserPersonalData`, los tests de `HmacUserLoginKeyFactory`, los tests de ids separados, el inicializador de índices y el HCL del capítulo 5. Los 16 borrados son los estados/claiming antiguos, los resolvers de estado de identidad y el enum `ExternalIdentityProvider`. Este documento de revisión también aparece como nuevo en `git status`, pero no forma parte del cambio funcional.

## 2. Orden de revisión recomendado

Revisa en este orden. Cada fase se apoya en la anterior; si cambias de orden, vas a leer código que usa conceptos que aún no has visto.

| Fase | Qué | Tiempo orientativo | Por qué primero |
|---|---|---|---|
| 0 | Baseline: build, tests, `emhcl validate`, `git status` | 10 min | Saber que partes de un estado verde |
| 1 | Contratos/eventos | 20 min | Es el vocabulario de todo lo demás |
| 2 | Auth y credenciales | 30 min | Explica cómo se obtiene el `user_id` |
| 3 | Estados, documentos y projectores de soporte | 30 min | Explica qué se guarda y qué se proyecta |
| 4 | Flujos de onboarding (initiate/alias/complete/cancel/retrieve) | 60 min | Es el núcleo del rediseño |
| 5 | Personal data y erasure | 45 min | Es la parte nueva y la más delicada |
| 6 | Read models de usuario/directorio | 20 min | Depende de 3 y 5 |
| 7 | Wiring de API/DI/configuración | 15 min | Une todo |
| 8 | HCL | 45 min | Contrato de comportamiento |
| 9 | Docs y slice records | 20 min | Deben reflejar decisiones, no comportamiento |
| 10 | Tests | 45 min | Criterio de aceptación ejecutable |

## 3. Fase 0: baseline

Antes de leer diffs, comprueba el estado:

```bash
emhcl validate doc/event-model
dotnet test --project test/Tooyioo.Tests/Tooyioo.Tests.csproj
git status --short
```

Esperado:

- `doc/event-model valid` (con avisos de modeling, no errores).
- 191 tests pasados, 0 fallos.
- En `git status` no deben aparecer `.nuget-http-cache/`, `.test-run.log` ni `doc/tooyioo.em.html` (este último está ignorado).

Presta atención a:

- Si `dotnet test` falla con el límite de inotify, el assembly de tests es antiguo: `TestEnvironment.cs` activa polling al cargar el assembly. Reconstruye antes de ejecutar.
- Si `dotnet build` falla por NuGet en un entorno con caché de solo lectura, es un problema del entorno, no del repo.

## 4. Fase 1: contratos y eventos

Ficheros:

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserOnboardingDomainEvents.cs` | M | `UserOnboardingInitiated` queda vacío; `UserOnboardingCompleted` solo lleva `TermsAndConditionsVersion`; `UserOnboardingCanceled` lleva `ReasonCode`; desaparecen los campos PII y el `UserId` de Completed | Que no quede PII en el stream de onboarding; que el consentimiento siga en Completed; compatibilidad de esquema (no hay datos en producción) |
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserAliasClaimingDomainEvents.cs` | M | `UserOnboardingId` → `UserId` en `UserAliasClaimed`/`UserAliasReleased` | Que el claim de alias apunte al mismo `userId` que el stream de onboarding |
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserLoginClaimingDomainEvents.cs` | D | Sustituido por `UserLoginClaimingDomainEvents` | Que no queden referencias al tipo ni al nombre de evento antiguo |
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserLoginClaimingDomainEvents.cs` | A | `UserLoginClaimed(UserId, UserOnboardingId)` y `UserLoginReleased(UserId)` | Que el claim sea mínimo; que Released no borre el stream |
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserLoginLinkingDomainEvents.cs` | A | `UserLoginLinked(UserLoginKey, Kind, Issuer)` y `UserLoginUnlinked(UserLoginKey)` | Que el índice inverso guarde la clave hasheada, nunca el subject |
| `src/user-onboarding/Tooyioo.UserOnboarding.Contracts/UserPersonalDataDomainEvents.cs` | A | `UserPersonalDataRecorded`, `UserPersonalDataErasureRequested`, `Canceled`, `Applied` | Que sea el único sitio con PII; que `EraseAfterUtc` sea un instante absoluto, no una duración |

Comprobación rápida:

```bash
rg "external_identity_id|external_identity_provider|external_identity_issuer|user_onboarding_id" \
  src/user-onboarding/Tooyioo.UserOnboarding.Contracts
```

Debe salir vacío.

## 5. Fase 2: auth y credenciales

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/Tooyioo.Common/Claims.cs` | M | Solo queda `UserId`; se elimina `UserOnboardingId` | Que no quede ningún `Claims.UserOnboardingId` |
| `src/Tooyioo.Common/AuthorizationPolicies.cs` | M | Se elimina `UserOnboardingRequired`; quedan `ExternalIdentityRequired` y `UserRequired` | Que ningún endpoint siga pidiendo la policy borrada |
| `src/Tooyioo.Common/UserLoginKey.cs` | A | Record con `UserLoginKey`, `Kind`, `Issuer` | Que `UserLoginKey` sea la clave HMAC en Base64Url, sin prefijo |
| `src/Tooyioo.Common/IUserLoginKeyFactory.cs` | A | Interfaz `ForOidc(issuer, subject)` | Que el contrato no exponga el subject al resto de la app |
| `src/Tooyioo.Api/Infrastructure/Auth/UserLoginOptions.cs` | A | `Pepper` único | Que el pepper sea de 32+ bytes; no hay versionado |
| `src/Tooyioo.Api/Infrastructure/Auth/HmacUserLoginKeyFactory.cs` | A | HMAC-SHA256, Base64Url; usa el issuer validado | Que el JWT sea la única fuente del issuer |
| `src/Tooyioo.Api/Infrastructure/Auth/IssuerCanonicalizer.cs` | D | Eliminado; el issuer se compara exacto | Que no se reintroduzca normalización |
| `src/Tooyioo.Common/ExternalIdentity.cs` | M | Record `(Id, Issuer)`; sin enum de provider | Que el `Issuer` sea el `iss` crudo |
| `src/Tooyioo.Api/Infrastructure/Auth/ExternalIdentityClaimsParser.cs` | M | Solo extrae `iss`/`sub`; ya no hardcodea Google ni mapea a un enum | Que la validación de issuer siga en el middleware JWT, no en el parser |
| `src/Tooyioo.Api/Infrastructure/Auth/IUserLoginResolver.cs` | A | Interfaz `Resolve(ExternalIdentity) → Option<UserId>` | Que `None` signifique "credencial no reclamada", no error |
| `src/Tooyioo.Api/Infrastructure/Auth/UserLoginResolver.cs` | A | Deriva la clave, carga `UserLoginClaimingState`, devuelve `Some/None` | Que un stream liberado (`UserId` null) devuelva `None` |
| `src/Tooyioo.Api/Infrastructure/Auth/AuthExtensions.cs` | M | Bind/validación de `UserLoginOptions`, registro de factory/resolver, `OnTokenValidated` añade solo `user_id`, policies; `Issuer` desde `GoogleJwtOptions` y comentario de frontera JWT | Que no se añada ningún claim de onboarding; que `ExternalIdentityRequired` siga permitiendo initiate sin `user_id`; que el resolver no rompa el token si no hay claim |
| `src/Tooyioo.Api/Tooyioo.Api.csproj` | M | `InternalsVisibleTo` para testear el key factory real | Que no se use para saltarse el HTTP en tests de slices |
| `src/Tooyioo.Api/appsettings.json` | M | Sección `Google` con ClientId/Issuer y `UserLogin` con pepper de desarrollo | **Sustituir el pepper de desarrollo en entornos reales**; no commitear secretos reales |
| `src/Tooyioo.Api/Infrastructure/Auth/ExternalIdentityStatusResolver.cs` | D | Ya no se consulta el estado del onboarding en auth | Que no quede registro DI |
| `src/Tooyioo.Api/Infrastructure/Auth/ExternalIdentityStatusResult.cs` | D | Idem | Idem |
| `src/Tooyioo.Api/Infrastructure/Auth/IExternalIdentityStatusResolver.cs` | D | Idem | Idem |
| `src/Tooyioo.Common/ExternalIdentityProvider.cs` | D | Enum Google-only que solo usaba el parser; el issuer sale del token validado | Que no quede ninguna referencia |
| `src/Tooyioo.Api/Infrastructure/EventStoreStateRetriever/UserOnboardingDetailsRetriever.cs` | D | Sustituido por `UserPersonalDataDetailsRetriever` | Que no queden referencias |
| `src/Tooyioo.Api/Infrastructure/EventStoreStateRetriever/EventStoreDetailsRetrieverExtensions.cs` | M | Registra los nuevos retrievers | Que el lifetime sea coherente (scoped/transient) |

La credencial usa un único vocabulario: `user_login_key` / `kind` / `issuer` en HCL, y `UserLoginKey` / `Kind` / `Issuer` en C#. No hay `Value`, `credential_kind` ni `user_login_key_hash`.

### Decisión: pepper único

`UserLoginOptions` tiene un solo `Pepper` de larga vida y `HmacUserLoginKeyFactory` hashea con él. No hay versionado ni rotación. Cambiarlo cambia todas las claves y deja de resolver las credenciales existentes; si algún día se rota, será un flujo de migración (resolver con el pepper viejo, re-keyar en el login y retirar los streams antiguos), no un cambio de configuración. El README y `auth.md` lo dejan explícito.

## 6. Fase 3: estados, documentos y projectores de soporte

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/user-onboarding/Tooyioo.UserOnboarding/UserOnboardingState.cs` | M | Estado sin PII: `IsInitiated`, `Alias`, `IsComplete`, `IsCanceled`, `CancellationReasonCode` | Que la decisión de complete/cancel no dependa de datos personales |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserOnboardingDocument.cs` | D | Eliminado; el GET de onboarding lee el `UserOnboardingState` del stream | Que no quede ninguna proyección Mongo del onboarding |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserOnboardingProjector.cs` | D | Eliminado; no se registra en la suscripción de read models | Que el `UserProjector` siga manteniendo el directorio |
| `src/user/Tooyioo.User/Features/Support/UserDocument.cs` | M | `Id` es el `user_id`; añade `UserOnboardingId` y `Status` | Que `UserOnboardingId` sea el campo de join y que `Id` siga siendo el `user_id` |
| `src/user/Tooyioo.User/Features/Support/UserProjector.cs` | M | `Initiated` crea el doc con `Id = user_id`, `UserOnboardingId = stream id` y lee PII; alias/completed/canceled actualizan por filtro `UserOnboardingId`; erasure aplica `[erased]` | Que el orden initiated→alias/completed/canceled se respete; que los filtros encuentren el doc; que un rebuild tras truncate siga funcionando |
| `src/user/Tooyioo.User/Features/Support/IUserPersonalDataReader.cs` | A | Interfaz para que el módulo User lea PII sin depender del estado de onboarding | Que no se filtre PII a logs |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserLoginClaimingId.cs` | A | Id del stream de claim | Que use la clave hasheada, no el subject |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserLoginClaimingState.cs` | A | `UserId`, `UserOnboardingId`; Claimed/Released | Que Released deje `UserId` en null y que un nuevo claim lo sobrescriba |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserLoginLinkingId.cs` | A | Id del índice por `userId` | — |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserLoginLinkingState.cs` | A | Lista de credenciales; Linked/Unlinked | Que Unlinked elimine la credencial correcta |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserAliasClaimingId.cs` | A | Id del stream por alias | — |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserAliasClaimingState.cs` | A | `UserId`; Claimed/Released | Que Released permita reclamar el alias |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserPersonalDataId.cs` | A | Id del stream por `userId` | — |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserPersonalDataState.cs` | A | PII + `IsErasureRequested`, `EraseAfterUtc`, `IsErased`; Recorded/Requested/Canceled/Applied | Que el estado no se use como read model fuera de los projectores |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserPersonalDataDocument.cs` | A | Documento del read model de PII | Que sea el único read model que expone PII al usuario |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserPersonalDataProjector.cs` | A | Proyecta recorded/requested/canceled/applied; aplica `[erased]` | Que Applied anonimice también en rebuild |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/UserPersonalDataErasureOptions.cs` | A | `GracePeriod` (14 días por defecto) | Que el valor por defecto y la config sean coherentes |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/IUserPersonalDataDetailsRetriever.cs` | A | Interfaz para el join de onboarding → PII | Que devuelva `None` cuando esté borrado, no PII vacía |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/ClaimingExternalIdentityId.cs` | D | Sustituido por `UserLoginClaimingId` | Que no queden referencias |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/ClaimingExternalIdentityState.cs` | D | Idem | Idem |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/ClaimingUserAliasId.cs` | D | Sustituido por `UserAliasClaimingId` | Idem |
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/Support/ClaimingUserAliasState.cs` | D | Idem | Idem |
| `src/user/Tooyioo.User/Features/Support/IUserOnboardingDetailsRetriever.cs` | D | Sustituido por `IUserPersonalDataReader` / `IUserPersonalDataDetailsRetriever` | Que no queden referencias |
| `src/user/Tooyioo.User/Features/Support/UserOnboardingDetails.cs` | D | Idem | Idem |

## 7. Fase 4: flujos de onboarding

### 7.1 Initiate

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/user-onboarding/Tooyioo.UserOnboarding/Features/InitiateUserOnboarding/InitiateUserOnboardingHandler.cs` | M | Carga el claim; si ya está reclamado devuelve ambos ids; valida que los otros tres streams estén vacíos; escribe 4 streams atómicos con `user_onboarding_id` y `user_id` | Orden de los streams; versiones esperadas; que un reintento no acuñe ids nuevos; que un claim liberado sí los acuñe |
| `.../InitiateUserOnboardingMappers.cs` | M | Acuña `userId`; parsea identidad/PII; construye `UserLoginKey`; mapea la respuesta | Que la PII vaya al comando y no al evento de onboarding |
| `.../InitiateUserOnboardingEndpoint.cs` | M | Sigue usando `ExternalIdentityRequired`; descripción actualizada | Que initiate sea el único endpoint que no exige `user_id` |
| `.../Contracts/InitiateUserOnboardingResponse.cs` | M | Devuelve `UserId` | Que el cliente no vea `userOnboardingId` |

### 7.2 Complete

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../CompleteUserOnboarding/CompleteUserOnboardingHandler.cs` | M | No acuña id; valida alias y estado; idempotente si ya está completo; escribe solo `UserOnboardingCompleted`; devuelve el `UserId` de la persona | Que un reintento devuelva el mismo `UserId`; que la ruta sea el `user_onboarding_id`; que no permita completar cancelado o sin alias |
| `.../CompleteUserOnboardingMappers.cs` | M | Mapea `userId` y terms; respuesta | Que no se lea PII |
| `.../CompleteUserOnboardingAuthorizer.cs` | M | Autoriza por `Claims.UserId` | Que la ruta sea el `userId` |
| `.../Contracts/CompleteUserOnboardingResponse.cs` | M | Solo `UserId` | — |
| `.../Contracts/CompleteUserOnboardingRoute.cs` | M | Ruta por `userId` | — |

### 7.3 Choose alias

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../ChooseUserOnboardingAlias/ChooseUserOnboardingAliasHandler.cs` | M | Carga onboarding y claim de alias; escribe `UserOnboardingAliasChosen` + `UserAliasClaimed` atómicos; idempotente con el mismo alias; rechaza alias distinto/reclamado/fuera de progreso | Que el alias ya elegido no se pueda cambiar; que un alias liberado se pueda reclamar |
| `.../ChooseUserOnboardingAliasMappers.cs` | M | `userId` y alias; respuesta | — |
| `.../ChooseUserOnboardingAliasAuthorizer.cs` | M | `Claims.UserId` | — |
| `.../Contracts/ChooseUserOnboardingAliasResponse.cs` | M | `UserId`, alias | — |
| `.../Contracts/ChooseUserOnboardingAliasRoute.cs` | M | Ruta por `userId` | — |

### 7.4 Cancel

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../CancelUserOnboarding/CancelUserOnboardingHandler.cs` | M | Cancela, pide erasure inmediato, desvincula y libera las credenciales del índice por `UserId`, libera el alias; idempotente si ya estaba cancelado | Que use el `UserId` del state (no el id del stream) para PII y credenciales; que las liberaciones sean un solo append |
| `.../CancelUserOnboardingMappers.cs` | M | `ReasonCode`; respuesta | Que el motivo sea un código acotado, no texto libre |
| `.../CancelUserOnboardingAuthorizer.cs` | M | `Claims.UserId` | — |
| `.../CancelUserOnboardingEndpoint.cs` | M | Descripción actualizada; ya no promete idempotencia pública | Que tras liberar la credencial el reintento sea 403, no 200 |
| `.../Contracts/CancelUserOnboardingRequest.cs` | M | `Reason` → `ReasonCode`; `reason_code_required`/`reason_code_too_long` | Que el cliente envíe `reasonCode`; que el validador use la misma clave |
| `.../Contracts/CancelUserOnboardingResponse.cs` | M | `UserId` | — |
| `.../Contracts/CancelUserOnboardingRoute.cs` | M | Ruta por `userId` | — |

### 7.5 Retrieve user onboarding

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../RetrieveUserOnboarding/RetrieveUserOnboardingHandler.cs` | M | Lee `UserOnboardingState` del stream y hace join de PII con `IUserPersonalDataDetailsRetriever`; si está borrado devuelve `[erased]` | Que no se lea PII del stream de onboarding; que no dependa de Mongo |
| `.../RetrieveUserOnboardingMappers.cs` | M | Respuesta `UserId`, PII o `[erased]`, `CancellationReasonCode` | Que el `UserId` sea el id del stream |
| `.../RetrieveUserOnboardingAuthorizer.cs` | M | `Claims.UserId` | — |
| `.../Contracts/RetrieveUserOnboardingResponse.cs` | M | `UserId`, estado, PII enmascarada, motivo | — |
| `.../Contracts/RetrieveUserOnboardingRoute.cs` | M | Ruta por `userId` | — |

## 8. Fase 5: User Personal Data y erasure

### 8.1 Retrieve

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../RetrieveUserPersonalData/RetrieveUserPersonalDataHandler.cs` | A | Consulta el doc de PII por `userId` | Que solo el dueño autenticado pueda leerlo |
| `.../RetrieveUserPersonalDataMappers.cs` | A | Mapea PII/estado/due date | Que no se loguee la respuesta |
| `.../RetrieveUserPersonalDataEndpoint.cs` | A | `GET /users/{user_id}/personal-data` | Que use `UserRequired` + autorizer de ruta |
| `.../RetrieveUserPersonalDataAuthorizer.cs` | A | Compara ruta y `Claims.UserId` | — |
| `.../Contracts/RetrieveUserPersonalDataResponse.cs` | A | PII, `Status`, `EraseAfterUtc` | — |
| `.../Contracts/RetrieveUserPersonalDataRoute.cs` | A | Ruta por `userId` | — |

### 8.2 Request erasure

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../RequestUserPersonalDataErasure/RequestUserPersonalDataErasureHandler.cs` | A | Valida PII existente/no borrada; idempotente si ya hay request; escribe `UserPersonalDataErasureRequested` con `now + grace` | Que una segunda petición no cambie la fecha; que una PII borrada no se pueda volver a pedir |
| `.../RequestUserPersonalDataErasureMappers.cs` | A | Respuesta con estado/due date | — |
| `.../RequestUserPersonalDataErasureEndpoint.cs` | A | `POST /users/{user_id}/personal-data/erase` | Que use POST, no GET |
| `.../RequestUserPersonalDataErasureAuthorizer.cs` | A | `Claims.UserId` | — |
| `.../Contracts/RequestUserPersonalDataErasureRequest.cs` | A | `ReasonCode` | Que el motivo sea código |
| `.../Contracts/RequestUserPersonalDataErasureResponse.cs` | A | `UserId`, `Status`, `EraseAfterUtc` | — |
| `.../Contracts/RequestUserPersonalDataErasureRoute.cs` | A | Ruta por `userId` | — |

### 8.3 Cancel erasure

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../CancelUserPersonalDataErasure/CancelUserPersonalDataErasureHandler.cs` | A | Valida PII; si no hay request devuelve OK sin evento; si está borrada error; escribe `UserPersonalDataErasureCanceled` | Que cancelar dos veces no duplique eventos |
| `.../CancelUserPersonalDataErasureMappers.cs` | A | Respuesta `active` | — |
| `.../CancelUserPersonalDataErasureEndpoint.cs` | A | `POST /users/{user_id}/personal-data/cancel-erasure` | — |
| `.../CancelUserPersonalDataErasureAuthorizer.cs` | A | `Claims.UserId` | — |
| `.../Contracts/CancelUserPersonalDataErasureRequest.cs` | A | Sin cuerpo | — |
| `.../Contracts/CancelUserPersonalDataErasureResponse.cs` | A | `UserId`, `Status` | — |
| `.../Contracts/CancelUserPersonalDataErasureRoute.cs` | A | Ruta por `userId` | — |

### 8.4 Apply erasure

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `.../ApplyUserPersonalDataErasure/ApplyUserPersonalDataErasureHandler.cs` | A | Carga PII; valida no borrada/request/due; encuentra la revisión del request; escribe applied + unlink/release; **trunca antes del request** | Que el append sea antes del truncate; que el truncate use la revisión correcta; que un reintento tras truncate sea idempotente |
| `src/Tooyioo.Api/Infrastructure/UserPersonalData/UserPersonalDataErasureScheduler.cs` | A | Cada 5 s consulta docs con `erase_after_utc` vencido y despacha el apply | Que un fallo en un doc no pare el resto; que no haya bucles de error sin backoff |
| `src/Tooyioo.Api/Infrastructure/Slicent/SlicentExtensions.cs` | M | Bind de opciones, registro del scheduler y del `UserPersonalDataProjector` | Que el scheduler no se registre en el host de tests (se eliminan hosted services) |

### 8.5 Nota sobre truncate

`ApplyUserPersonalDataErasureHandler` hace:

1. append de `UserPersonalDataErasureApplied` (y releases),
2. `TruncateStream` con la posición del `UserPersonalDataErasureRequested`.

En KurrentDB, `$tb` conserva el evento en la posición de truncate y borra los anteriores; el request y el applied sobreviven. En `InMemoryEventStore`, el truncate elimina también el evento de la posición (off-by-one), así que el test del apply verifica el applied y la liberación de credenciales, no que el request sobreviva. Está anotado en `doc/guides/testing.md`.

## 9. Fase 6: read models de usuario y directorio

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/user/Tooyioo.User/Features/RetrieveUser/Contract/RetrieveUserResponse.cs` | M | Sin `ExternalIdentity*` ni `UserOnboardingId`; añade `Status` | Que el contrato público no exponga identificadores de proveedor |
| `src/user/Tooyioo.User/Features/RetrieveUser/RetrieveUserMappers.cs` | M | Mapea `Status` y PII enmascarada | — |
| `src/user/Tooyioo.User/Features/RetrieveUsers/Contract/RetrieveUsersResponse.cs` | M | Ídem para la lista | — |
| `src/user/Tooyioo.User/Features/RetrieveUsers/RetrieveUsersHandler.cs` | M | Filtra `Status == completed` | Que un usuario cancelado/borrado no aparezca |
| `src/user/Tooyioo.User/Features/RetrieveUsers/RetrieveUsersMappers.cs` | M | Mapea `Status` | — |
| `src/user/Tooyioo.User/Features/RetrieveUserSummaries/RetrieveUserSummariesHandler.cs` | M | Filtra completed | — |
| `src/user/Tooyioo.User/Features/RetrieveUserSummaries/RetrieveUserSummariesMappers.cs` | M | Mapea `Status`/resumen | — |

## 10. Fase 7: wiring de API/DI

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `src/Tooyioo.Api/Infrastructure/Slicent/SlicentExtensions.cs` | M | Opciones de erasure, scheduler, projector de PII y registro del inicializador de índices | Que el projector se registre en la suscripción correcta; que el índice no se cree en tests (hosted services eliminados) |
| `src/Tooyioo.Api/Infrastructure/Mongo/MongoReadModelIndexInitializer.cs` | A | Crea el índice único parcial de `UserDocument.UserOnboardingId` | Que sea único y que excluya el valor vacío |
| `src/Tooyioo.Api/Infrastructure/EventStoreStateRetriever/EventStoreDetailsRetrieverExtensions.cs` | M | Registra `UserPersonalDataDetailsRetriever` y `UserPersonalDataReader` | Que no queden registros del retriever borrado |
| `src/Tooyioo.Api/Infrastructure/EventStoreStateRetriever/UserPersonalDataDetailsRetriever.cs` | A | Lee el estado de PII para el join de onboarding | Que devuelva `None` si está borrado |
| `src/Tooyioo.Api/Infrastructure/EventStoreStateRetriever/UserPersonalDataReader.cs` | A | Implementa `IUserPersonalDataReader` para el projector de User | Que no exponga PII fuera del projector |
| `src/Tooyioo.Api/appsettings.json` | M | `UserLogin` | Ver nota de pepper |
| `src/user/Tooyioo.User/Tooyioo.User.csproj` | M | `NoWarn` incluye `EVTC001` | Es una supresión consciente por `[DomainEventType]`, no un fallo |

## 11. Fase 8: HCL

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `doc/event-model/00-catalog.em.hcl` | M | Board, actores, contextos `user_personal_data`/`authentication`/`user_onboarding`/`business_onboarding`, eventos nuevos y flags `pii` | Que el catálogo no tenga eventos que el código no escriba (salvo los de business ya existentes) |
| `doc/event-model/00-chapters.em.hcl` | M | 5 capítulos; chapter 5 de PII/erasure | Que el orden del board y los capítulos coincida |
| `doc/event-model/01-user_onboarding.em.hcl` | M | Reescrito: initiate, retrieve user login, retrieve onboarding, choose alias, retrieve alias, complete, project users; `user_onboarding_id` en rutas/readmodel de onboarding y `user_id` en el evento iniciado y en las respuestas | Que el id del stream sea `user_onboarding_id` y el de la persona `user_id`; que cada escenario tenga un test |
| `doc/event-model/02-user_onboarding_cancellation.em.hcl` | M | Reescrito: cancel + releases + erasure request; escenarios | Que el escenario de cancelación incluya el erasure request |
| `doc/event-model/03-user_directory.em.hcl` | M | `retrieve_users`, `retrieve_user_summaries`, `retrieve_user_profile`; campos sin external/onboarding id y con `status` | Que el read model `users` sea coherente con el de `project_users` |
| `doc/event-model/05-user_personal_data.em.hcl` | A | Capítulo 5: retrieve/request/cancel/apply + automatización | Que el apply describa el truncate y que los escenarios cubran due/not due/not requested |
| `doc/event-model/00-hotspots.em.hcl` | D | El hotspot de mapeo de claims ya está resuelto por el user login key | — |
| `doc/event-model/README.md` | M | Capítulos 01–05; sin hotspots; nota de truncate en testing | — |
| `doc/event-model/04-business_onboarding.em.hcl` | (sin cambios) | Business onboarding no se ha tocado | Sus eventos de tax/contact/completed/canceled existen en código pero no en HCL; es deuda previa, no de este rediseño |
| `doc/proposals/user-onboarding-and-erasure.em.hcl` | A | Propuesta de diseño, marcada como superseded | Decidir si se queda como registro o se borra antes de commitear |
| `doc/proposals/user-onboarding-and-erasure.html` | A | Diagrama generado de la propuesta | No revisar línea a línea; decidir si se commitea (es generado) |

## 12. Fase 9: documentación y slice records

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `README.md` | M | Nota del pepper de login y del issuer de Google | Que no se confunda con el ClientSecret de Scalar |
| `doc/architecture/auth.md` | M | Reescrito: user login key, único `user_id`, rotación, issuer evolution | Que la nota de rotación coincida con lo que hace el código (ver fase 2) |
| `doc/architecture/event-sourcing.md` | M | Excepción de `user_personal_data`; 4 streams; eventos de ejemplo | Que la excepción quede clara frente a "streams son procesos" |
| `doc/guides/testing.md` | M | Auth por `user_id`, helpers de PII, polling de inotify, off-by-one del truncate | — |
| `doc/index.md` | M | Enlace al slice de erasure | — |
| `doc/slices/initiate-user-onboarding.md` | M | Decisiones del nuevo modelo (PII, user login key, 4 streams) | Que no duplique el HCL |
| `doc/slices/complete-user-onboarding.md` | M | Sin acuñar id; consentimiento; idempotencia | — |
| `doc/slices/cancel-user-onboarding.md` | M | Erasure inmediato, releases, no idempotencia pública | — |
| `doc/slices/choose-user-onboarding-alias.md` | M | `userId` y claim de alias | — |
| `doc/slices/initiate-business-onboarding.md` | M | `InitiatedBy` = `user_id` | — |
| `doc/slices/personal-data-erasure.md` | A | Decisiones: PII única, truncate, gracia, scheduler, credenciales | Que el enlace de tests apunte a `Features/UserPersonalData` |

## 13. Fase 10: tests

### 13.1 Soporte

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `test/Tooyioo.Tests/Support/Events/DomainEvent.cs` | M | Builders nuevos para PII/credenciales/erasure; compatibilidad (`UserLoginClaimed` → `UserLoginClaimed`, `.WithUserOnboardingId` → `UserId`); se elimina `WithExternalIdentityProvider` (muerto) | Que los builders no oculten un cambio de contrato; que `WithUserOnboardingId` siga siendo un alias explícito |
| `test/Tooyioo.Tests/Support/Events/WhenDomainEventsAreSerialized.cs` | M | Round-trip de los nuevos eventos | Que el nombre persistido no cambie |
| `test/Tooyioo.Tests/Support/VerticalSliceTestHost.cs` | M | `GivenUserPersonalData`, `GivenLinkedLogin`, `SendCommand` | Que `SendCommand` no se use para saltarse el HTTP en tests que deben probar el endpoint |
| `test/Tooyioo.Tests/Support/VerticalSliceWebApplicationFactory.cs` | M | Registra el fake de `IUserLoginKeyFactory` | Que el fake no se filtre a tests de auth real |
| `test/Tooyioo.Tests/Support/Auth/TestJwtBearerOptions.cs` | M | Ahora valida el issuer contra el issuer de test (antes `ValidateIssuer = false`) | Es lo que permite que el parser sea raw; `UnsupportedIssuer` se rechaza en JWT, como en producción |
| `test/Tooyioo.Tests/Support/TestUserLoginKeyFactory.cs` | A | Clave = subject | Que el test siembre el claim con el subject, no con HMAC |
| `test/Tooyioo.Tests/Support/TestEnvironment.cs` | A | Activa `DOTNET_USE_POLLING_FILE_WATCHER` | Es la solución al límite de inotify; no es código de producción |
| `test/Tooyioo.Tests/Features/Support/UserReadModelQueryTest.cs` | M | Siembra PII para los tests de directorio | Que los IDs de usuario sean los IDs de onboarding |

### 13.2 Tests de infraestructura de auth

| Fichero | Estado | Qué cambió | Presta atención a |
|---|---|---|---|
| `test/Tooyioo.Tests/Infrastructure/Auth/IssuerCanonicalizerTests.cs` | D | Eliminado; el issuer se compara exacto | Que no quede tolerancia de esquema |
| `test/Tooyioo.Tests/Infrastructure/Auth/HmacUserLoginKeyFactoryTests.cs` | A | Issuer del token; subjects distintos | Que el key factory real se pruebe sin el fake de los slices |

### 13.3 Tests por feature

| Grupo | Ficheros | Qué cambió | Presta atención a |
|---|---|---|---|
| Cancel | `CancelUserOnboardingAuthenticationTests.cs`, `CancelUserOnboardingValidationTests.cs`, `WhenAnotherUserTriesToCancelUserOnboarding.cs`, `WhenUserOnboardingIsAlreadyCanceled.cs`, `WhenUserOnboardingIsCompleted.cs`, `WhenUserOnboardingWithAliasCanBeCanceled.cs`, `WhenUserOnboardingWithoutAliasCanBeCanceled.cs` | `userId`; `ReasonCode`; claves `reasonCode`; se siembra PII e índice; 403 tras liberar credencial | Que el test de "already canceled" espere 403 (la credencial ya no resuelve), no 200 |
| Choose alias | `ChooseUserOnboardingAliasValidationTests.cs`, `WhenAnotherUserTriesToChooseAliasForUserOnboarding.cs`, `WhenCanceledUserOnboardingChoosesAlias.cs`, `WhenCompletedUserOnboardingChoosesAlias.cs`, `WhenUserOnboardingAliasIsAlreadyInUse.cs`, `WhenUserOnboardingAliasIsAvailable.cs`, `WhenUserOnboardingAliasWasReleased.cs` | Renombrados y `userId` | Que el alias liberado se pueda reclamar |
| Complete | `CompleteUserOnboardingValidationTests.cs`, `WhenAnotherUserTriesToCompleteUserOnboarding.cs`, `WhenUserOnboardingCanBeCompleted.cs`, `WhenUserOnboardingDoesNotExist.cs`, `WhenUserOnboardingHasNoAlias.cs`, `WhenUserOnboardingIsAlreadyCompleted.cs`, `WhenUserOnboardingIsCanceled.cs`, `WhenUserIdDiffersFromUserOnboardingId.cs` | `userId`; el nuevo test prueba ids distintos: ruta por onboarding id, respuesta por user id | Que el authorizer compare `state.UserId` con el claim; que el id del stream no sea el user id |
| Initiate | `WhenGoogleIdentityIsAlreadyClaimed.cs`, `WhenGoogleIdentityIsUnknown.cs`, `WhenGoogleIdentityWasReleased.cs` | Claim por subject; `userId`; 4 streams | Que el reintento devuelva el mismo `userId` y no escriba |
| Business | `InitiateBusinessOnboardingValidationTests.cs`, `WhenBusinessOnboardingIsInitiated.cs`, `WhenBusinessOnboardingExists.cs`, `WhenBusinessOnboardingIsNotProjected.cs` | `InitiatedBy`/`userId` = onboarding id | Que el negocio siga usando el `user_id` del claim |
| Retrieve user | `WhenUserExists.cs`, `WhenUserProfileIsNotProjected.cs`, `WhenUserIdDiffersFromUserOnboardingId.cs` | `userId`; status; el nuevo test prueba que el `UserDocument` se keya por user id y no por onboarding id | El 404 debe ocurrir después del 200 de auth; el `_userId` del test debe ser el del claim |
| Retrieve onboarding | `WhenCanceledUserOnboardingExists.cs`, `WhenUserOnboardingDoesNotExist.cs` | Sin proyección Mongo: el cancelado da 403 porque el login se liberó; el inexistente da 404; ambos usan el stream | Que no se reintroduzca `QueryService<UserOnboardingDocument>` |
| Retrieve users | `WhenUsersAreFilteredByAliasContainsIgnoringCase.cs`, `WhenUsersAreFilteredByEmailIgnoringCase.cs`, `WhenUsersAreFilteredByEmailVerification.cs`, `WhenUsersAreFilteredByIdContainsIgnoringCase.cs`, `WhenUsersAreRetrievedWithoutFilters.cs` | IDs de usuario = IDs de onboarding; status | Que los IDs esperados sean los del stream, no GUIDs inventados |
| Retrieve summaries | `WhenUserSummariesAreFilteredByAliasContainsIgnoringCase.cs`, `WhenUserSummariesAreFilteredByIdContainsIgnoringCase.cs` | IDs de usuario | Ídem |
| UserPersonalData | `WhenUserPersonalDataIsRetrieved.cs`, `WhenUserPersonalDataErasureIsRequested.cs`, `WhenUserPersonalDataErasureIsCanceled.cs`, `WhenDueUserPersonalDataErasureIsApplied.cs` | Tests nuevos | El de apply usa el in-memory store; ver nota de truncate |

## 14. Comprobaciones transversales

Ejecuta estas búsquedas antes de dar el cambio por bueno:

```bash
# 1. El id del proceso y el de la persona deben ser tipos separados
grep -rn "UserOnboardingId" src --exclude-dir=obj --exclude-dir=bin

# 2. No deben quedar los claiming antiguos
grep -rnE "ClaimingExternalIdentity|ClaimingUserAlias|UserLoginClaiming" src test doc --exclude-dir=obj --exclude-dir=bin

# 3. No debe quedar el enum de provider antiguo
grep -rn "ExternalIdentityProvider" src test --exclude-dir=obj --exclude-dir=bin

# 4. No debe quedar la policy vieja
grep -rn "UserOnboardingRequired" src test --exclude-dir=obj --exclude-dir=bin

# 5. La PII solo debe aparecer en UserPersonalDataDomainEvents
grep -rnE "\b(Name|LastName|Email)\b" src/user-onboarding/Tooyioo.UserOnboarding.Contracts --exclude-dir=obj --exclude-dir=bin

# 6. El HCL y los tests deben seguir verdes
emhcl validate doc/event-model
dotnet test --project test/Tooyioo.Tests/Tooyioo.Tests.csproj
```

Matices:

- `UserOnboardingId` es el id del stream/agregado `UserOnboarding-{user_onboarding_id}`. El `UserId` es la persona y se guarda en `UserOnboardingInitiated`; pueden coincidir o no. No hay que eliminarlo.
- `ExternalIdentity`/`ExternalIdentityClaimsParser` siguen existiendo: son la identidad cruda del token (`iss`/`sub`), no el claim de negocio.

## 15. Puntos que requieren una decisión (no son bugs de compilación)

1. **Truncate y concurrencia**: el apply usa `ExpectedStreamVersion.Any` para el truncate. Si ya está borrado y no hay append, un cancel concurrente podría colarse. Hoy es improbable por el scheduler, pero conviene decidir si se revalida la versión antes del truncate.
2. **Business onboarding**: el código tiene eventos de tax/contact/completed/canceled que el HCL no modela. Es deuda previa; no la he tocado. Decide si entra en este cambio o en otro.
3. **`doc/proposals`**: la propuesta y su HTML están sin trackear y marcados como superseded. Decide si se commitean como registro o se borran.
4. **Datos existentes**: no hay datos en producción, así que los cambios de esquema son aceptables. Si los hubiera, `UserDocument.Status` requeriría backfill y los eventos viejos migración.
5. **Pepper de desarrollo**: `appsettings.json` lleva uno; en entornos reales hay que sustituirlo y guardarlo en el secret manager.

## 16. Qué no hace falta revisar línea a línea

- `doc/proposals/user-onboarding-and-erasure.html` y `doc/tooyioo.em.html`: generados.
- Tests de rename mecánico (`userOnboardingId` → `userId`): usa `git diff -w` y comprueba que no queden literales viejos con `rg`.
- `obj/` y `bin/`: no están en el diff.
- El `NoWarn` de `EVTC001` en `Tooyioo.User.csproj`: es una supresión consciente, no un cambio de comportamiento.

## 17. Orden de commit sugerido

Si vas a separar la revisión en commits:

1. Contratos/eventos.
2. Auth/credenciales y `Claims`/policies.
3. Estados, documentos y projectores de soporte.
4. Flujos de onboarding.
5. Personal data/erasure y scheduler.
6. Read models de usuario/directorio.
7. HCL.
8. Docs/slices.
9. Tests.

Antes de commitear, `git status --short` no debe contener `.nuget-http-cache/`, `.test-run.log` ni `doc/tooyioo.em.html` (ignorado). Decide qué hacer con `doc/proposals`.
