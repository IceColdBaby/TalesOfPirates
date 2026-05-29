# Tales of Pirates Unity port

The project aims to port D3D9 C++ game Tales of Pirates to Unity.

## unityMCP

unityMCP is available so you can interact with the Unity editor.

## Lessons learned

- Unity pins the version of the C# compiler it uses to C# 9.
- LSP diagnostics show no errors, but the code might not compile in Unity. Consider using unityMCP instead.
