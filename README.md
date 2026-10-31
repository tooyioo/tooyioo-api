# tooyioo-api

[![CI/CD](https://github.com/tooyioo/tooyioo-api/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/tooyioo/tooyioo-api/actions/workflows/ci-cd.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-TUnit-512BD4?logo=dotnet&logoColor=white)](https://tunit.dev/)
[![Test Reports](https://img.shields.io/badge/test%20reports-history-2ea44f?logo=github)](https://tooyioo.github.io/tooyioo-api/tests/)
[![Event Model](https://img.shields.io/badge/event%20model-view-b15b28)](https://tooyioo.github.io/tooyioo-api/event-model/)

Event Source application with vertical slice approach.

## Prerequisites

- .NET 10 SDK
- Podman 6.1.0+
- Access to the `tooyioo` project in [Google Cloud Console](https://console.cloud.google.com/)

## Getting started

### 1. Configure Google OAuth for Scalar

The API uses the `tooyioo-web` OAuth client from the `tooyioo` Google Cloud project.

The Google Client ID is already configured in `appsettings.json` and is used by the API to validate Google ID tokens.

Scalar also uses this OAuth client to authenticate with Google when testing the API locally. This requires the OAuth client secret, which must not be committed to source control.

From `src/Tooyioo.Api`, configure the client secret using .NET User Secrets:

```bash
dotnet user-secrets set "Scalar:GoogleOAuth:ClientSecret" "<google-oauth-client-secret>"
```

The secret can be obtained from the `tooyioo-web` OAuth client in Google Cloud Console.

Verify the local configuration:

```bash
dotnet user-secrets list
```

The `tooyioo-web` OAuth client must contain this authorized redirect URI:

```text
http://localhost:8000/scalar/oauth/callback
```

### 2. Start Podman

On macOS and Windows, start the Podman machine:

```bash
podman machine start
```

On Linux, Podman runs natively. Verify that it is available:

```bash
podman info
```

See [Podman and Testcontainers on Linux](#podman-and-testcontainers-on-linux) if integration tests cannot start containers.

### 3. Start the application

Run the Aspire AppHost project.

Aspire starts:

- Tooyioo API
- KurrentDB
- MongoDB
- Mongo Express

### 4. Access local services

Once Aspire has started:

- Scalar API Reference: [http://localhost:8000/scalar](http://localhost:8000/scalar)
- OpenAPI document: [http://localhost:8000/openapi/public.json](http://localhost:8000/openapi/public.json)
- KurrentDB: [http://localhost:2113](http://localhost:2113)
- Mongo Express: [http://localhost:27018](http://localhost:27018)

Use Scalar's authentication controls to sign in with Google. Scalar uses the Google OAuth Authorization Code flow with the `openid`, `email`, and `profile` scopes.

The API authenticates requests with the returned OpenID Connect `id_token`, not the OAuth `access_token`:

```text
Authorization: Bearer <id_token>
```

## Tests and reports

Restore the report generator tool:

```bash
dotnet tool restore
```

Run all tests with coverage and TRX reports:

```bash
dotnet test --coverage --report-trx
```

Generate the HTML coverage report:

```bash
dotnet tool run reportgenerator \
  -reports:**/TestResults/*.coverage \
  -targetdir:report \
  -reporttypes:Html
```

Open `report/index.html` to view the report.

## Published documentation

A push to `main` publishes the project pages to GitHub Pages:

- [`/`](https://tooyioo.github.io/tooyioo-api/) - landing page linking both artifacts
- [`/tests/`](https://tooyioo.github.io/tooyioo-api/tests/) - TUnit test history; the latest report is also available at [`/latest/`](https://tooyioo.github.io/tooyioo-api/latest/)
- [`/event-model/`](https://tooyioo.github.io/tooyioo-api/event-model/) - interactive event model canvas generated from `doc/event-model`

The test report and event model are published together in one commit to `gh-pages`, so there is no race between separate deployments. The generated event model HTML is not committed to the repository.

## Documentation model

The canonical artifact table is in [doc/index.md](doc/index.md). In short: the HCL event model is the source of truth for behavior, tests are its executable acceptance criteria, code is the implementation, and slice decision records capture only non-obvious decisions and constraints.

When behavior changes, update HCL first, then tests and code. Update a slice decision record only when a decision changes. See [Slice decision records](doc/slices/README.md) for the template and [AGENTS.md](AGENTS.md) for agent rules.

## Interacting with the API

### Scalar

Scalar is the recommended way to explore and test individual API endpoints during development.

After configuring the Google OAuth secret, open:

```text
http://localhost:8000/scalar
```

Scalar handles Google authentication and automatically uses the resulting `id_token` as the Bearer token for authenticated endpoints.

### Bruno

Bruno is recommended for more complex or repeatable scenarios, such as chaining requests or reusing values from previous responses.

Enable `Use System Browser for OAuth2 Authorization` under `Preferences > General`.

The collection reads the Google ID token from Bruno's OAuth store:

```text
{{$oauth2.google-credentials.id_token}}
```

## Event model

The event model lives in [`doc/event-model`](doc/event-model/) and is the source of truth for behavior. See [the event model README](doc/event-model/README.md) for installing or upgrading EMHCL, generating the diagram, and publishing it.

Quick render from the repository root:

```bash
./doc/generate-tooyioo-diagram.sh
```

## Troubleshooting and development notes

### Podman and Testcontainers on Linux

The integration tests use Testcontainers, which communicates with a Docker-compatible API. Rootless Podman exposes this API through a user socket, which is not enabled by default.

Enable the socket:

```bash
systemctl --user enable --now podman.socket
```

Configure Testcontainers to use it:

```bash
export DOCKER_HOST="unix:///run/user/$(id -u)/podman/podman.sock"
```

To make this persistent for Bash, add the export to `~/.bashrc` and reload it:

```bash
source ~/.bashrc
```

Verify the configuration:

```bash
echo "$DOCKER_HOST"
podman ps
```

When Rider is launched from the desktop, it may not inherit variables from `~/.bashrc`. Launch Rider from a configured terminal or add `DOCKER_HOST` to the test run configuration environment.

### MongoDB on Linux kernel 6.19+

Recent Linux kernels are incompatible with MongoDB's per-CPU TCMalloc cache. This affects local MongoDB containers because containers use the host kernel.

MongoDB containers used by Aspire and Testcontainers must set:

```text
GLIBC_TUNABLES=glibc.pthread.rseq=1:glibc.cpu.hwcaps=-SHSTK
```

`glibc.pthread.rseq=1` prevents MongoDB from enabling its per-CPU TCMalloc cache on affected kernels. `glibc.cpu.hwcaps=-SHSTK` is retained as a workaround for a separate MongoDB startup issue.

For Testcontainers, configure the Mongo builder as follows:

```csharp
// Keep glibc rseq enabled to prevent MongoDB's per-CPU TCMalloc cache on
// Linux kernels >= 6.19, where it cannot start reliably.
_container = new MongoDbBuilder(MongoImage)
    .WithEnvironment(
        "GLIBC_TUNABLES",
        "glibc.pthread.rseq=1:glibc.cpu.hwcaps=-SHSTK")
    .Build();
```

Apply the same `GLIBC_TUNABLES` value to the MongoDB resource configured in Aspire.

### Minimal API validation in multi-assembly setups

When using ASP.NET Minimal APIs built-in validation with endpoints and request DTOs defined in different assemblies, or endpoints mapped indirectly through shared generic helpers, two additional steps are required due to limitations in the validation source generator.

1. Annotate request DTOs with `[ValidatableType]`.

   This forces the validation source generator to include a DTO even when it does not appear directly in a Minimal API handler signature.

2. Include an `AddValidation()` trigger in the DTO or feature assembly.

   A non-referenced helper such as `ValidationCodegenTrigger` is required so that the validation source generator runs for that assembly and emits its validation metadata.

This is a known limitation of the current Minimal API validation source generator when endpoints and DTOs live in separate assemblies.

Further details: [Stack Overflow question](https://stackoverflow.com/q/79855763/2948212).