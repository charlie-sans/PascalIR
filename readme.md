# PascalIR

PascalIR is a Pascal-to-ObjectIR compiler and toolkit for generating, analyzing,
and executing an intermediate representation of Pascal programs. It helps
developers, researchers, and educators produce ObjectIR modules for
transformation, serialization, and runtime execution.

## Overview

PascalIR is a research / tooling codebase that provides:
- a Pascal lexer, parser and compiler front-end (`PascalIR` project)
- an Object IR implementation and serialization utilities (`ObjectIR.Core`)
- examples and runtime helpers under `examples/` and `OJRuntime`

The project is organized as a .NET solution and includes a command-line
compiler/runtime that can parse Pascal source and emit or execute an IR.

## Features

- Pascal lexical analysis and parsing
- AST construction and compilation to an intermediate representation
- IR serialization (JSON/BSON) and module loading
- Example Pascal sources and small runtime harnesses

## Repository Layout

- [PascalIR](PascalIR): compiler front-end and CLI entry point.
- [ObjectIR.Core](ObjectIR.Core): core IR types, instructions, module composition, and serialization.
- [examples](examples): sample Pascal programs and corresponding OIR files.
- OJRuntime: minimal runtime utilities used by some examples.
- PascalIR.sln: Visual Studio / dotnet solution for building the whole workspace.

Key files:
- `PascalIR/PascalCompiler.cs` — compiler driver and pipeline.
- `PascalIR/PascalParser.cs`, `PascalIR/PascalLexer.cs` — language front-end.
- `ObjectIR.Core/Module.cs`, `ObjectIR.Core/Instruction.cs` — IR model and instructions.

## Build & Run

From the repository root you can build the full solution with the .NET SDK:

```bash
dotnet build PascalIR.sln
```

Run the PascalIR project (compile or inspect example files):

```bash
dotnet run --project PascalIR -- <path/to/source.pas>
```

Example (compile an example file):

```bash
dotnet run --project PascalIR -- examples/main.pas
```

Binaries and build artifacts will appear under the usual `bin/` and `obj/`
folders for each project.

## Examples

See the `examples/` folder for sample Pascal sources and prebuilt `.oir` files
demonstrating the compiler pipeline and runtime usage.

## Contributing

Contributions are welcome. Typical workflows:

1. Fork the repository
2. Create a feature branch
3. Submit a pull request with tests or example programs demonstrating changes

Please open issues for bugs or feature requests.

## License

No license file is included in this repository. If you plan to publish or
collaborate broadly, add a `LICENSE` file describing the intended license.

---

If you want, I can also run a build to verify everything builds on your machine,
or tweak the README to include more detailed developer notes or examples.
