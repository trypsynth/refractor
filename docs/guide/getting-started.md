# Getting started

Add the package:

```shell
dotnet add package Refractor
```

Everything starts from a <xref:Refractor.Prism>, which finds and creates backends. A <xref:Refractor.Backend> is one speech engine or screen reader.

```csharp
using Refractor;

using Prism prism = new();
using Backend backend = prism.CreateBest();
backend.Speak("Hello from .NET!");
```

`Speak` returns as soon as speech starts. Pass `interrupt: true` to stop whatever the backend is saying first. Without it, most backends queue the new text after the old.

## Choosing a backend

There are four ways to get one:

| Method | State | Initialized on return |
| --- | --- | --- |
| <xref:Refractor.Prism.CreateBest> | Yours alone | Yes |
| <xref:Refractor.Prism.Create(Refractor.BackendId)> | Yours alone | No, call `Initialize()` |
| <xref:Refractor.Prism.AcquireBest> | Shared with other callers | Yes |
| <xref:Refractor.Prism.Acquire(Refractor.BackendId)> | Shared with other callers | Maybe, call `Initialize()` |

Use `CreateBest` unless you need something else. It tries each registered backend, highest priority first, and returns the first that starts. Running screen readers have the highest priorities, so a user who has one running hears your app through it.

`Acquire` and `AcquireBest` return a shared instance, so a voice, rate or pitch set through one handle is seen through every other. Use them only when sharing that state is what you want. <xref:Refractor.Backend.Initialize> returns `false` when the backend was already initialized, which is not an error.

To ask for a particular backend, use its id from <xref:Refractor.BackendId>, or find it by name:

```csharp
if (prism.TryFindBackend("NVDA", out BackendId nvda)) {
	using Backend backend = prism.Create(nvda);
	backend.Initialize();
	backend.Speak("Through NVDA");
}
```

<xref:Refractor.Prism.GetBackendIds> lists every registered backend, highest priority first. Registered does not mean usable: a screen reader that is not running is registered all the same.

`CreateBest` and `AcquireBest` skip backends that report <xref:Refractor.BackendFeatures.RequiresExplicitSelection>, so those only come by id. Two are built in. <xref:Refractor.BackendId.Clipboard> puts text on the clipboard, and <xref:Refractor.BackendId.NoSpeech> accepts every call and makes no sound, which is handy in tests.

## What a backend can do

Not every backend supports every operation. A screen reader can speak and often braille, but has no voices to choose from. A speech engine can synthesize to memory, but has no braille. <xref:Refractor.Backend.GetFeatures> says what a backend has:

```csharp
if (backend.Supports(BackendFeatures.Pause)) backend.Pause();
```

<xref:Refractor.BackendFeatures.IsScreenReader> and <xref:Refractor.BackendFeatures.IsSpeechEngine> tell the two kinds apart. `GetFeatures` can probe the system, so keep its result rather than calling it in a loop.

## Errors

Every failure prism reports throws <xref:Refractor.PrismException>, whose <xref:Refractor.PrismException.Error> says what went wrong. An operation the backend does not have throws with <xref:Refractor.PrismError.NotImplemented>, and a backend that cannot start throws with <xref:Refractor.PrismError.BackendNotAvailable>.

```csharp
try {
	backend.Rate = 0.8f;
} catch (PrismException error) when (error.Error == PrismError.NotImplemented) {
	// This backend keeps the user's rate.
}
```

## Voices, rate, pitch and volume

<xref:Refractor.Backend.Volume>, <xref:Refractor.Backend.Rate> and <xref:Refractor.Backend.Pitch> each run from 0 to 1. A rate of 0.5 is always the backend's default. <xref:Refractor.Backend.GetVoices> lists the voices, each with its language when the backend reports one, and <xref:Refractor.Backend.VoiceIndex> picks one:

```csharp
foreach (Voice voice in backend.GetVoices()) Console.WriteLine($"{voice.Index}: {voice.Name} ({voice.Language})");
backend.VoiceIndex = 2;
```

A voice's index can change after <xref:Refractor.Backend.RefreshVoices>, so store a voice's name, not its index.

## Synthesizing to memory

A speech engine can hand you its audio instead of playing it. <xref:Refractor.Backend.SpeakToMemory(System.String,Refractor.AudioCallback)> calls back with interleaved 32 bit samples in [-1, 1], and returns once all of it is delivered:

```csharp
List<float> samples = [];
int sampleRate = 0;
backend.SpeakToMemory("Hello", (chunk, channels, rate) => {
	samples.AddRange(chunk.ToArray());
	sampleRate = rate;
});
```

The samples are only valid during the callback, so copy what you need. The callback can run on another thread, and an exception it throws is rethrown from `SpeakToMemory`.

## Threads and lifetime

A backend is not thread safe. Use one instance from one thread at a time, or create one per thread. A `Prism` is safe to use from any thread.

Dispose what you create. A backend stays usable after the `Prism` that made it is disposed, and disposing a backend may or may not stop its speech, so call <xref:Refractor.Backend.Stop> first to be sure.

On macOS, iOS and Android, some backends need the main thread to be free. See [Platforms](platforms.md).
