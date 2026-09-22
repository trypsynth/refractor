namespace Refractor;

/// <summary>A backend's stable identifier, hashed by prism from its name.</summary>
public readonly record struct BackendId(ulong Value) {
	public static BackendId Invalid => default;
	public static BackendId Sapi => new(0x1D6DF72422CEEE66);
	public static BackendId AVSpeech => new(0x28E3429577805C24);
	public static BackendId VoiceOver => new(0xCB4897961A754BCB);
	public static BackendId SpeechDispatcher => new(0xE3D6F895D949EBFE);
	public static BackendId Nvda => new(0x89CC19C5C4AC1A56);
	public static BackendId Jaws => new(0xAC3D60E9BD84B53E);
	public static BackendId OneCore => new(0x6797D32F0D994CB4);
	public static BackendId Orca => new(0x10AA1FC05A17F96C);
	public static BackendId AndroidScreenReader => new(0xD199C175AEEC494B);
	public static BackendId AndroidTts => new(0xBC175831BFE4E5CC);
	public static BackendId WebSpeech => new(0x3572538D44D44A8F);
	public static BackendId Uia => new(0x6238F019DB678F8E);
	public static BackendId Zdsr => new(0x3D93C56C9E7F2A2E);
	public static BackendId ZoomText => new(0xAE439D62DC7B1479);
	public static BackendId BoyPCReader => new(0x285ABA1C16F3300F);
	public static BackendId PCTalker => new(0x344B951962E3B835);
	public static BackendId SenseReader => new(0xED4760890B55C2F2);
	public static BackendId SystemAccess => new(0x8380F2A37B2C3EB6);
	public static BackendId WindowEyes => new(0x9120D89908785C13);
	public static BackendId Spiel => new(0x478B44F14AD3D89C);

	public bool IsValid => Value != 0;

	public override string ToString() => $"0x{Value:X16}";
}
