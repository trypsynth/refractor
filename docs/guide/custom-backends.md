# Custom backends

A backend written in .NET sits in prism's registry beside the built-in ones, so `CreateBest`, availability polling and everything else treat it the same. Derive from <xref:Refractor.CustomBackend>, override the operations it supports, and register it with a <xref:Refractor.RegistryBuilder>:

```csharp
using RegistryBuilder builder = new();
BackendId id = builder.AddBackend("Console", 100, BackendFeatures.Speak, () => new ConsoleBackend());
using Registry registry = builder.Freeze();
using Prism prism = new(new PrismOptions { Registry = registry });

using Backend backend = prism.Create(id);
backend.Initialize();
backend.Speak("Hello");

sealed class ConsoleBackend : CustomBackend {
	public override void Speak(string text, bool interrupt) => Console.WriteLine(text);
}
```

## Features

The features passed to <xref:Refractor.RegistryBuilder.AddBackend*> must name exactly the operations the backend overrides. prism only calls those, and the rest report <xref:Refractor.PrismError.NotImplemented> to whoever asks. Each operation's feature is named after it: overriding `SpeakToMemory` means declaring <xref:Refractor.BackendFeatures.SpeakToMemory>.

Features can also say what kind of backend it is. <xref:Refractor.BackendFeatures.IsScreenReader> or <xref:Refractor.BackendFeatures.IsSpeechEngine> classifies it, though not both, and <xref:Refractor.BackendFeatures.RequiresExplicitSelection> keeps `CreateBest` from picking it.

The priority decides where the backend falls in `CreateBest`'s order. Running screen readers are around 100, and system voices a little below.

## Availability

Override <xref:Refractor.CustomBackend.IsSupported> to say whether the engine can be used right now, such as whether a program it talks to is running. prism calls it before <xref:Refractor.CustomBackend.Initialize>, and from its poll thread when availability is being watched.

## Errors

Throw <xref:Refractor.PrismException> to report a particular error, such as <xref:Refractor.PrismError.BackendNotAvailable> from `Initialize` when the engine is not there. `NotImplementedException` and `NotSupportedException` become `NotImplemented`, `ArgumentOutOfRangeException` and `OverflowException` become `RangeOutOfBounds`, and `ArgumentException` becomes `InvalidParameter`. Anything else becomes <xref:Refractor.PrismError.Internal>, and is written to prism's log.

## Threads and lifetime

prism never calls one instance from two threads at once, but calls can arrive on any thread. The factory passed to `AddBackend` makes one instance per backend prism creates, and returning null fails that creation. If the backend implements `IDisposable`, it is disposed when prism destroys the instance.

## Synthesizing to memory

<xref:Refractor.CustomBackend.SpeakToMemory(System.String,Refractor.AudioSink)> receives an <xref:Refractor.AudioSink>. Write every sample to it before returning:

```csharp
public override void SpeakToMemory(string text, AudioSink sink) {
	float[] samples = Synthesize(text);
	sink.Write(samples, channels: 1, sampleRate: 22050);
}
```

The sink only works during the call. A `Write` after `SpeakToMemory` returns throws `ObjectDisposedException`.

## Plugin libraries

<xref:Refractor.RegistryBuilder.AddLibrary*> loads a prism plugin, a native library with backends of its own, and registers every backend in it. This runs the library's own code, so only load libraries you trust.
