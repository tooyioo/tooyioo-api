# Tooyioo event model

This folder contains the Tooyioo model as `.em.hcl` files and supporting images. The diagram-generation scripts live in the parent `doc/` folder and use the `emhcl` executable from `PATH`. The generated diagram is written only to `doc/tooyioo.em.html`, which is ignored by Git.

The model targets Event Modeling HCL v0.5.0 and requires `emhcl` 0.12.0 or newer. `emhcl` treats all direct child `.em.hcl` files as one model; file names and block order no longer define the story order. For an older v0.4.0 model, run `emhcl migrate -w <model-or-folder>` before validating.

- `00-catalog.em.hcl` contains the `board`, actors, and the bounded context.
- `01-chapters.em.hcl` contains chapter membership. Chapter list order is not story order; `board.workflows` sets the order used by the diagram.
- `10-*.em.hcl` and `20-*.em.hcl` contain one command or view workflow per file.
- `20-project_users.em.hcl` defines the shared `Users` projection from `UserOnboardingCompleted`. The Chapter 3 directory queries in `20-retrieve_users.em.hcl` and `20-retrieve_user_summaries.em.hcl` query that projection.

The `board` block lists the workflows in business-story order. Workflows omitted from the board remain valid but render separately as off-board workflows.

From the repository root, run `./doc/generate-tooyioo-diagram.sh` on Linux or macOS. On Windows, run `doc\generate-tooyioo-diagram.bat`; it invokes the same `.sh` through Git for Windows Bash. From WSL, run the `.sh` directly.

Both scripts require `emhcl` 0.12.0 or newer on `PATH`. To run the commands individually:

```bash
emhcl validate doc/event-model
emhcl diagram doc/event-model -o doc/tooyioo.em.html
```

The renderer reports modeling warnings without blocking HTML generation.

CI regenerates this diagram and publishes it to GitHub Pages at `/event-model/`. The output is not committed to the repository.
