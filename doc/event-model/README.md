# Tooyioo event model

This folder contains the Tooyioo model as `.em.hcl` files. It is the source of truth for system behavior. [Slice decision records](../slices/README.md) capture non-obvious decisions and constraints, not a second copy of behavior.

## Model layout

The model targets Event Modeling HCL v0.5.0 and requires `emhcl` 0.12.0 or newer.

- `00-catalog.em.hcl` contains the `board`, actors, and bounded contexts.
- `00-chapters.em.hcl` contains chapter membership. Chapter list order is not story order; `board.workflows` sets the order used by the diagram. The viewer orders chapters by chapter ID; zero-padded prefixes such as `chapter_01`, `chapter_02`, ... keep the picker ordered past 10.
- `00-hotspots.em.hcl` contains global hotspots.
- `01-user_onboarding.em.hcl` through `04-business_onboarding.em.hcl` contain one chapter each, with its workflows in board order. The file prefix is for human navigation only; it does not define model order.
- `04-business_onboarding.em.hcl` starts the Business Onboarding bounded context in Chapter 4.
- `01-user_onboarding.em.hcl` contains the shared `Users` projection from `UserOnboardingCompleted`; the Chapter 3 directory queries in `03-user_directory.em.hcl` read that projection.

`emhcl` treats all direct child `.em.hcl` files as one model; file names and block order no longer define story order. It ignores subfolders, so model files must stay directly in this folder; use `chapter` blocks for grouping. For an older v0.4.0 model, run `emhcl migrate -w <model-or-folder>` before validating.

The `board` block lists workflows in business-story order. Workflows omitted from the board remain valid but render separately as off-board workflows.

Scenario IDs use zero-padded prefixes (`scenario_01`, `scenario_02`, ...). The viewer orders scenarios by ID, so happy paths that return events or read models appear before error scenarios.

## Adding a chapter or workflow

The model is a flat folder; EMHCL ignores subfolders. Keep the convention:

- `00-*` are global files: catalog, chapters, and hotspots.
- `NN-<chapter>.em.hcl` contains all workflows for chapter `NN`, in board order.
- Current convention is one file per chapter, not one file per workflow.
- File prefixes are for human navigation only; `board.workflows` and `chapter` define model order.

To add a workflow to an existing chapter:

1. Add `workflow.<id>` to `board.workflows` at the right story position.
2. Add it to the chapter's `workflows` list in `00-chapters.em.hcl`.
3. Add the workflow block to the chapter's `NN-<chapter>.em.hcl` file at the board position.
4. Add the scenarios and their tests.
5. Update the slice decision record only if a decision or constraint changed.

To add a chapter:

1. Add `chapter_NN_<name>` to `00-chapters.em.hcl`.
2. Create `NN-<chapter>.em.hcl`.
3. Add its workflows to the board and to the chapter's `workflows` list.
4. Keep `NN` zero-padded so the picker orders past 10.

## Install or upgrade EMHCL

Download the **0.12.0 or newer compatible** release from the [EMHCL releases](https://github.com/event-modeling-hcl/eventmodeling-hcl/releases/tag/v0.12.0).

Regardless of the operating system, the executable must be named `emhcl` (or `emhcl.exe` on Windows) and be available on `PATH`; the scripts call `emhcl` directly.

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

On Windows, download the `windows_amd64` release, extract `emhcl.exe`, and add its directory to `PATH`. The `.bat` wrapper also requires Git for Windows Bash.

Verify on either OS:

```bash
emhcl version
```

It should report `emhcl v0.12.0` or newer.

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
