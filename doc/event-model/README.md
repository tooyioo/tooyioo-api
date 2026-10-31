# Tooyioo event model

This folder contains the Tooyioo model as `.em.hcl` files. It is the source of truth for system behavior. [Slice decision records](../slices/README.md) capture non-obvious decisions and constraints, not a second copy of behavior.

## Model layout

The model targets Event Modeling HCL v0.5.0 and requires `emhcl` 0.12.0 or newer.

- `00-catalog.em.hcl` contains the `board`, actors, and the bounded context.
- `01-chapters.em.hcl` contains chapter membership. Chapter list order is not story order; `board.workflows` sets the order used by the diagram. The viewer orders chapters by chapter ID; zero-padded prefixes such as `chapter_01`, `chapter_02`, ... keep the picker ordered past 10.
- `10-*.em.hcl` and `20-*.em.hcl` contain one command or view workflow per file.
- `10-initiate_business_onboarding.em.hcl` starts the Business Onboarding bounded context in Chapter 4.
- `20-project_users.em.hcl` defines the shared `Users` projection from `UserOnboardingCompleted`. The Chapter 3 directory queries in `20-retrieve_users.em.hcl` and `20-retrieve_user_summaries.em.hcl` query that projection.

`emhcl` treats all direct child `.em.hcl` files as one model; file names and block order no longer define story order. For an older v0.4.0 model, run `emhcl migrate -w <model-or-folder>` before validating.

The `board` block lists workflows in business-story order. Workflows omitted from the board remain valid but render separately as off-board workflows.

## Install or upgrade EMHCL

Download the **0.12.0 or newer compatible** release from the [EMHCL releases](https://github.com/event-modeling-hcl/eventmodeling-hcl/releases/tag/v0.12.0).

On Linux or macOS, install the `emhcl` executable in `~/.local/bin`:

```bash
mkdir -p ~/.local/bin
install -m 755 ~/Downloads/emhcl ~/.local/bin/emhcl
```

Ensure `~/.local/bin` is on `PATH` if it is not already:

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

To upgrade an existing Ubuntu installation from the repository root:

```bash
EMHCL_INSTALL_DIR="$HOME/.local/bin" EMHCL_VERSION=0.12.0 bash scripts/install-emhcl.sh
hash -r
```

Verify:

```bash
emhcl version
```

On Windows, download the `windows_amd64` release, extract `emhcl.exe`, and add its directory to `PATH`. The `.bat` wrapper also requires Git for Windows Bash.

CI installs the pinned version in [`.github/workflows/ci-cd.yml`](../../.github/workflows/ci-cd.yml), verifies the release checksum, and fails if the pinned version cannot be installed.

## Generate the diagram

From the repository root:

```bash
./doc/generate-tooyioo-diagram.sh
```

On Windows, run `doc\generate-tooyioo-diagram.bat`; it invokes the same `.sh` through Git for Windows Bash. From WSL, run the `.sh` directly.

To run the commands individually:

```bash
emhcl validate doc/event-model
emhcl diagram doc/event-model -o doc/tooyioo.em.html
```

The generated diagram is written only to `doc/tooyioo.em.html`, which is ignored by Git. The renderer reports modeling warnings without blocking HTML generation.

CI regenerates the diagram and publishes it to GitHub Pages at `/event-model/`. The output is not committed to the repository.
