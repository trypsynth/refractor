namespace Refractor.Tests;

// The backends prism compiles in differ by operating system, so tests that name one ask here.
internal static class Platform {
	internal static (BackendId Id, string Name)[] ExpectedBackends =>
		OperatingSystem.IsWindows() ? [(BackendId.Sapi, "SAPI"), (BackendId.Nvda, "NVDA"), (BackendId.Jaws, "JAWS"), (BackendId.OneCore, "OneCore"), (BackendId.ZoomText, "ZoomText")]
		: OperatingSystem.IsMacOS() ? [(BackendId.AVSpeech, "AVSpeech"), (BackendId.VoiceOver, "VoiceOver")]
		: [];

	internal static (BackendId Id, string Name) AnyBackend => ExpectedBackends[0];
}
