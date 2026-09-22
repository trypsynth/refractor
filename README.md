# Refractor

.NET bindings to [prism](https://github.com/ethindp/prism), which puts screen readers and speech engines behind one API. On Windows that means NVDA, JAWS, ZoomText, SAPI, OneCore, UI Automation and more. On macOS and iOS it means VoiceOver and AVSpeech. Refractor covers the whole prism C API. It works with Native AOT, and links prism into the executable so that it ships as one file.

## Quick start

```csharp
using Refractor;

using Prism prism = new();
using Backend backend = prism.CreateBest();
backend.Speak("Hello from .NET!");
```

`CreateBest` picks the highest priority backend that starts. When a screen reader is running, that is the screen reader, so speech goes through the user's own voice and settings. Otherwise it falls back to a system voice.

## Picking a backend

| Method | State | Initialized on return |
| --- | --- | --- |
| `CreateBest()` | Yours alone | Yes |
| `Create(id)` | Yours alone | No, call `Initialize()` |
| `AcquireBest()` | Shared with other callers | Yes |
| `Acquire(id)` | Shared with other callers | Maybe, call `Initialize()` |

Use `CreateBest` unless you need something else. `Initialize` returns `false` when the backend was already initialized, which is not an error. Ids for every known backend are on `BackendId`, for example `BackendId.Nvda`.

Not every backend supports every operation. Check `GetFeatures()` or `Supports(...)` first. An unsupported operation throws `PrismException` with `PrismError.NotImplemented`.

A backend is not thread safe. Use one instance from one thread at a time, or create one per thread.

## Knowing when a screen reader starts or stops

```csharp
using Prism prism = new(new PrismOptions {
	AvailabilityChanged = (id, name, isAvailable) => Console.WriteLine($"{name}: {isAvailable}")
});
```

The callback runs on prism's poll thread. A backend that is already running when polling starts is not reported.

## Custom backends

Derive from `CustomBackend`, override the operations you support, and name the same operations in the features you register with:

```csharp
using RegistryBuilder builder = new();
BackendId id = builder.AddBackend("Console", 100, BackendFeatures.Speak, () => new ConsoleBackend());
using Registry registry = builder.Freeze();
using Prism prism = new(new PrismOptions { Registry = registry });

sealed class ConsoleBackend : CustomBackend {
	public override void Speak(string text, bool interrupt) => Console.WriteLine(text);
}
```

`RegistryBuilder.AddLibrary` loads prism plugin libraries the same way.

## Logging

```csharp
PrismLog.SetLevel(LogLevel.Warning);
PrismLog.SetHandler((level, source, message) => Console.Error.WriteLine($"{level} {source}: {message}"));
```

Setting the `PRISM_LOG` environment variable to `warn`, `debug` and so on makes prism log to standard error without any code.

## Native AOT

With `PublishAot` on, Refractor links prism statically. On Windows it also delay loads the few screen reader DLLs that most machines do not have. The published executable needs nothing beside it. Set `RefractorStaticLink` to `false` to ship prism next to the executable instead, as `prism.dll` on Windows or `libprism.dylib` on macOS.

Without Native AOT, prism loads from the package's `runtimes` folder.

## Platforms

| Platform | Runtimes | Backends | Oldest version |
| --- | --- | --- | --- |
| Windows | `win-x64`, `win-arm64`, `win-x86` | NVDA, JAWS, ZoomText, SAPI, OneCore, UI Automation and more | Windows 10 |
| macOS | `osx-arm64`, `osx-x64` | VoiceOver, AVSpeech | macOS 12 |
| iOS | `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` | VoiceOver, AVSpeech | iOS 14 |

An iOS app cannot load a library of its own, so Refractor always links prism into the app there. There is nothing to set up.

Linux and Android support is in progress.

## The raw API

`Refractor.Interop.PrismNative` exposes every prism function, struct and constant one to one, including the plugin ABI types, for anything the safe types do not cover.

## Building

Prism is a git submodule, built with CMake. Windows needs Visual Studio with the C++ tools, and macOS and iOS need Xcode. The build script takes the runtimes to build, and defaults to `win-x64`:

```powershell
git clone --recurse-submodules https://github.com/trypsynth/refractor
cd refractor
./scripts/build-native.ps1 -Runtimes osx-arm64
dotnet test -p:RefractorTestRuntime=osx-arm64
```

`dotnet pack` needs prism built for every runtime in the package, so CI builds the packages.

## License

Refractor is MIT licensed. The package also contains prism, which is under the Mozilla Public License 2.0, and the libraries prism uses. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
