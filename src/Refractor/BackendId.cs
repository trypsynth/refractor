namespace Refractor;

/// <summary>
/// A backend's stable identifier, hashed by prism from its name. An id here only says a backend
/// exists somewhere; <see cref="Prism.HasBackend"/> says whether this platform's prism has it.
/// </summary>
/// <param name="Value">prism's 64 bit identifier.</param>
public readonly record struct BackendId(ulong Value) {
	/// <summary>No backend. Always 0.</summary>
	public static BackendId Invalid => default;
	/// <summary>Microsoft SAPI speech, on Windows.</summary>
	public static BackendId Sapi => new(0x1D6DF72422CEEE66);
	/// <summary>Apple's AVSpeechSynthesizer, on macOS, iOS and Apple's other platforms.</summary>
	public static BackendId AVSpeech => new(0x28E3429577805C24);
	/// <summary>The VoiceOver screen reader, on macOS, iOS and Apple's other platforms.</summary>
	public static BackendId VoiceOver => new(0xCB4897961A754BCB);
	/// <summary>Speech Dispatcher, on Linux and BSD.</summary>
	public static BackendId SpeechDispatcher => new(0xE3D6F895D949EBFE);
	/// <summary>The NVDA screen reader, on Windows.</summary>
	public static BackendId Nvda => new(0x89CC19C5C4AC1A56);
	/// <summary>The JAWS screen reader, on Windows.</summary>
	public static BackendId Jaws => new(0xAC3D60E9BD84B53E);
	/// <summary>Windows OneCore speech, on Windows 10 and later.</summary>
	public static BackendId OneCore => new(0x6797D32F0D994CB4);
	/// <summary>The Orca screen reader, on Linux and BSD.</summary>
	public static BackendId Orca => new(0x10AA1FC05A17F96C);
	/// <summary>The running Android screen reader, such as TalkBack.</summary>
	public static BackendId AndroidScreenReader => new(0xD199C175AEEC494B);
	/// <summary>Android's text to speech engine.</summary>
	public static BackendId AndroidTts => new(0xBC175831BFE4E5CC);
	/// <summary>The browser's Web Speech API, on the web.</summary>
	public static BackendId WebSpeech => new(0x3572538D44D44A8F);
	/// <summary>UI Automation notifications, which Windows screen readers announce, on Windows.</summary>
	public static BackendId Uia => new(0x6238F019DB678F8E);
	/// <summary>The Zhengdu screen reader, on Windows.</summary>
	public static BackendId Zdsr => new(0x3D93C56C9E7F2A2E);
	/// <summary>ZoomText, on Windows.</summary>
	public static BackendId ZoomText => new(0xAE439D62DC7B1479);
	/// <summary>The BoyPC Reader screen reader, on Windows.</summary>
	public static BackendId BoyPCReader => new(0x285ABA1C16F3300F);
	/// <summary>The PC-Talker screen reader, on Windows.</summary>
	public static BackendId PCTalker => new(0x344B951962E3B835);
	/// <summary>The Sense Reader screen reader, on Windows.</summary>
	public static BackendId SenseReader => new(0xED4760890B55C2F2);
	/// <summary>The System Access screen reader, on Windows, in builds of prism with its legacy backends.</summary>
	public static BackendId SystemAccess => new(0x8380F2A37B2C3EB6);
	/// <summary>The Window-Eyes screen reader, on Windows, in builds of prism with its legacy backends.</summary>
	public static BackendId WindowEyes => new(0x9120D89908785C13);
	/// <summary>The Spiel speech framework, on Linux and BSD. Refractor's Linux builds leave it out.</summary>
	public static BackendId Spiel => new(0x478B44F14AD3D89C);
	/// <summary>Microsoft GDK speech, on Windows and Xbox. Refractor's Windows builds leave it out.</summary>
	public static BackendId GdkSpeech => new(0xD39D872349416B11);
	/// <summary>Puts text on the system clipboard. Only created when asked for by id or name.</summary>
	public static BackendId Clipboard => new(0x7056908DE8F9F991);
	/// <summary>Accepts every operation and produces nothing, silence for speech to memory. Only created when asked for by id or name.</summary>
	public static BackendId NoSpeech => new(0xC2C0EAB0BF97A5F0);

	/// <summary>Whether this is any id other than <see cref="Invalid"/>.</summary>
	public bool IsValid => Value != 0;

	/// <summary>The id in hexadecimal, such as 0x89CC19C5C4AC1A56.</summary>
	public override string ToString() => $"0x{Value:X16}";
}
