# VncSharpCore runtime

`VncSharpCore.dll` is built from the official mRemoteNG VncSharpCore repository.

- Source: https://github.com/mRemoteNG/VncSharpCore
- Commit: `ee43141d288561c77023de1a996099dbb5ae55e2`
- Target: `net6.0-windows`, Release
- License: GPL-2.0-only

This revision restores the embedded `Resources/vnccursor.cur` file that is
missing from NuGet package `VncSharpCore` 1.2.1. Without it, a successful VNC
handshake fails while constructing the connected-state cursor with
`ArgumentNullException: Value cannot be null. (Parameter 'stream')`.

Build command:

```powershell
dotnet build .\VncSharpCore\VncSharpCore.csproj -c Release
```
