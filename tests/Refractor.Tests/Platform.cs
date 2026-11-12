namespace Refractor.Tests;

// The backends prism compiles in differ by operating system, so tests that name one ask here.
internal static class Platform {
	internal static (BackendId Id, string Name)[] ExpectedBackends =>
		OperatingSystem.IsWindows() ? [(BackendId.Sapi, "SAPI"), (BackendId.Nvda, "NVDA"), (BackendId.Jaws, "JAWS"), (BackendId.OneCore, "OneCore"), (BackendId.ZoomText, "ZoomText"), (BackendId.Clipboard, "Clipboard"), (BackendId.NoSpeech, "No Speech")]
		: OperatingSystem.IsMacOS() ? [(BackendId.AVSpeech, "AVSpeech"), (BackendId.VoiceOver, "VoiceOver"), (BackendId.Clipboard, "Clipboard"), (BackendId.NoSpeech, "No Speech")]
		: OperatingSystem.IsLinux() ? [(BackendId.SpeechDispatcher, "Speech Dispatcher"), (BackendId.Orca, "Orca"),(BackendId.Clipboard, "Clipboard"), (BackendId.NoSpeech, "No Speech")]
		: [];

	internal static (BackendId Id, string Name) AnyBackend => ExpectedBackends[0];
}
