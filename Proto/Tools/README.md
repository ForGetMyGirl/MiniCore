# MiniCore Proto Tools

`protoc 29.5` is bundled for Windows x64, macOS Intel, and macOS Apple Silicon.
Unity selects the matching executable automatically through `MiniCore/Protocol/Generate All`.

The executables are official Protocol Buffers release artifacts. Standard definitions under
`protoc-29.5/include/google/protobuf` are available to project Proto imports but are excluded
from MiniCore business-message generation.

Source: https://github.com/protocolbuffers/protobuf/releases/tag/v29.5
