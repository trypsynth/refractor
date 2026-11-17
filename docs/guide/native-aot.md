# Native AOT

Refractor works with Native AOT. With `PublishAot` on, it links prism into the executable statically, so the published app is one file with nothing beside it:

```xml
<PropertyGroup>
	<PublishAot>true</PublishAot>
</PropertyGroup>
```

```shell
dotnet publish -r win-x64
```

To ship prism beside the executable instead, as `prism.dll` on Windows, `libprism.dylib` on macOS or `libprism.so` on Linux, set `RefractorStaticLink` to `false`.

Without Native AOT, prism loads from the package's `runtimes` folder, and there is nothing to set up.

## What each platform needs

* **Windows** needs the C++ tools that Native AOT itself needs, from Visual Studio.
* **macOS** needs Xcode's command line tools.
* **Linux** links with clang, so it needs `clang` and `zlib1g-dev` or their equivalents. prism also needs `libdbus-1`, which every desktop distribution has.

## iOS and Android

An iOS app cannot load a library of its own, so Refractor always links prism into it, whether or not it uses Native AOT.

Android is the other way around: prism's Android backends are written in Java as well as C++, so Android always uses prism's shared library, Native AOT or not.
