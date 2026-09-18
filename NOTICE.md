# Third-party notices

GleamFarmer bundles the following third-party components. Their licenses are
reproduced by reference (SPDX identifiers); full texts are available at the
listed URLs.

| Component | Version | License | Source |
|---|---|---|---|
| Gleam compiler (WASM build) | 1.18.1 | Apache-2.0 | https://github.com/gleam-lang/gleam |
| gleam_stdlib | 1.0.5 | Apache-2.0 | https://github.com/gleam-lang/gleam_stdlib |
| Wasmtime (.NET + native) | 44.0.0 | Apache-2.0 WITH LLVM-exception | https://github.com/bytecodealliance/wasmtime-dotnet |
| Jint | 4.16.2 | BSD-2-Clause | https://github.com/sebastienros/jint |
| Acornima | 1.7.0 | BSD-3-Clause | https://github.com/adams85/acornima |
| IndexRange | 1.0.2 | MIT | https://github.com/bgrainger/IndexRange |

Runtime dependency libraries shipped for .NET Framework compatibility
(`System.Memory`, `System.Buffers`, `System.Numerics.Vectors`,
`System.Runtime.CompilerServices.Unsafe`, `System.ValueTuple`) are licensed
under the MIT License by the .NET Foundation.

**Not bundled:** BepInEx (LGPL-2.1, installed separately by the user) and the
game's own assemblies (`Core.dll`, `Utils.dll`, UnityEngine, Unity.TextMeshPro),
which are proprietary and loaded from the player's game installation.
