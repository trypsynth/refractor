---
_layout: landing
---

# Refractor

Refractor is a set of .NET bindings to [prism](https://github.com/ethindp/prism), which puts screen readers and speech engines behind one API. It gives a .NET app speech, braille and screen reader output on Windows, macOS, iOS, Linux and Android: NVDA, JAWS, ZoomText, SAPI, OneCore, VoiceOver, AVSpeech, Speech Dispatcher, Orca, TalkBack, Android text to speech and more.

Refractor covers the whole prism C API. It works with Native AOT, and links prism into the executable so that an app ships as one file.

## Install

```shell
dotnet add package Refractor
```

## Speak

```csharp
using Refractor;

using Prism prism = new();
using Backend backend = prism.CreateBest();
backend.Speak("Hello from .NET!");
```

`CreateBest` picks the best backend that starts. When a screen reader is running, that is the screen reader, so speech goes through the user's own voice and settings. Otherwise it falls back to a system voice.

## Next

* [Getting started](guide/getting-started.md) covers choosing a backend, what it can do, and its errors.
* [Platforms](guide/platforms.md) lists the backends on each platform, and what each one needs.
* The [API reference](api/index.md) documents every type and member.

Refractor is MIT licensed, and its source is on [GitHub](https://github.com/trypsynth/refractor).
