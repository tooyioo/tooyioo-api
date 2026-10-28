# Tooyioo event model

This folder contains the Tooyioo model as `.em.hcl` files and supporting images. The `emhcl` executable and diagram-generation script live in the parent `doc/` folder. The generated diagram is written only to `doc/tooyioo.em.html`.

`emhcl` treats all direct child `.em.hcl` files as one model and reads them in filename order.

- `00-catalog.em.hcl` contains actors and the bounded context.
- `01-chapters.em.hcl` contains the chapter and its workflow order. Keep all chapter blocks in this file.
- `10-*.em.hcl` and `20-*.em.hcl` contain one command or view workflow per file.

Run `..\generate-tooyioo-diagram.bat` to validate and generate the diagram. To run the commands individually from the repository root:

```powershell
.\doc\emhcl.exe validate .\doc\event-model
.\doc\emhcl.exe diagram .\doc\event-model -o .\doc\tooyioo.em.html
```

The renderer reports modeling warnings without blocking HTML generation.
