# Platforms

| Platform | Runtimes | Backends | Oldest version |
| --- | --- | --- | --- |
| Windows | `win-x64`, `win-arm64`, `win-x86` | NVDA, JAWS, ZoomText, SAPI, OneCore, UI Automation and more | Windows 10 |
| macOS | `osx-arm64`, `osx-x64` | VoiceOver, AVSpeech | macOS 12 |
| iOS | `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` | VoiceOver, AVSpeech | iOS 14 |
| Linux | `linux-x64`, `linux-arm64` | Speech Dispatcher, Orca | glibc 2.35, as in Ubuntu 22.04 |
| Android | `android-arm64`, `android-x64` | The running screen reader, such as TalkBack, and Android text to speech | Android 8 (API 26), with `net10.0-android` |

Every platform also has the Clipboard and No Speech backends, which are only used when asked for by id. <xref:Refractor.Prism.HasBackend*> says whether a backend is built in on the platform you are running on.

## Windows

prism talks to each screen reader the way it expects, loading the screen reader's own library from where it is installed. Nothing has to ship with your app.

## macOS and iOS

VoiceOver and AVSpeech do their work on the main thread, so a call from another thread waits for the main thread to run its run loop, as it always does in an app with a user interface. A console app has no run loop, so it should make every backend call from its main thread. In an `async Main`, that means before the first `await`, since the code after it usually continues on another thread. If the main thread is blocked waiting for such a call, the call never returns.

On macOS, VoiceOver speech works without any setup. To use VoiceOver's older automation path, an app bundle must declare `NSAppleEventsUsageDescription` in its `Info.plist`. A notarized app also needs the `com.apple.security.automation.apple-events` entitlement. Sandboxed apps cannot use this path.

An iOS app cannot load a library of its own, so Refractor always links prism into the app there. There is nothing to set up.

## Linux

prism needs `libdbus-1`, which every desktop distribution has. Speech goes through Speech Dispatcher, so it has to be installed with a voice, as it is on most desktops. Orca is reached over D-Bus while it runs.

## Android

Target `net10.0-android`. Refractor brings prism's library archive, which merges into your app's manifest and sets prism up when the app starts.

Starting Android's speech engine waits for a callback on the main thread, so call `CreateBest`, `AcquireBest` and `Initialize` from another thread, or the app hangs for 10 seconds and the backend fails to start:

```csharp
protected override void OnCreate(Bundle? savedInstanceState) {
	base.OnCreate(savedInstanceState);
	Task.Run(() => {
		using Prism prism = new();
		using Backend backend = prism.CreateBest();
		backend.Speak("Hello from Android.");
	});
}
```

Apps that shrink their code with R8 keep the Java classes prism reaches from native code, through ProGuard rules Refractor includes.
