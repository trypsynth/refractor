namespace Refractor;

/// <summary>
/// What a backend implements, plus whether its engine is running right now. Each operation flag
/// names the <see cref="Backend"/> member it allows; a member whose flag is missing throws
/// <see cref="PrismException"/> with <see cref="PrismError.NotImplemented"/>.
/// </summary>
[Flags]
public enum BackendFeatures : ulong {
	/// <summary>No features.</summary>
	None = 0,
	/// <summary>The engine or screen reader is available. Advisory: initializing can still fail.</summary>
	IsSupportedAtRuntime = 1UL << 0,
	/// <summary><see cref="Backend.Speak"/>.</summary>
	Speak = 1UL << 2,
	/// <summary><see cref="Backend.SpeakToMemory"/>.</summary>
	SpeakToMemory = 1UL << 3,
	/// <summary><see cref="Backend.Braille"/>.</summary>
	Braille = 1UL << 4,
	/// <summary><see cref="Backend.Output"/>.</summary>
	Output = 1UL << 5,
	/// <summary><see cref="Backend.IsSpeaking"/>.</summary>
	IsSpeaking = 1UL << 6,
	/// <summary><see cref="Backend.Stop"/>.</summary>
	Stop = 1UL << 7,
	/// <summary><see cref="Backend.Pause"/>.</summary>
	Pause = 1UL << 8,
	/// <summary><see cref="Backend.Resume"/>.</summary>
	Resume = 1UL << 9,
	/// <summary>Setting <see cref="Backend.Volume"/>.</summary>
	SetVolume = 1UL << 10,
	/// <summary>Getting <see cref="Backend.Volume"/>.</summary>
	GetVolume = 1UL << 11,
	/// <summary>Setting <see cref="Backend.Rate"/>.</summary>
	SetRate = 1UL << 12,
	/// <summary>Getting <see cref="Backend.Rate"/>.</summary>
	GetRate = 1UL << 13,
	/// <summary>Setting <see cref="Backend.Pitch"/>.</summary>
	SetPitch = 1UL << 14,
	/// <summary>Getting <see cref="Backend.Pitch"/>.</summary>
	GetPitch = 1UL << 15,
	/// <summary><see cref="Backend.RefreshVoices"/>.</summary>
	RefreshVoices = 1UL << 16,
	/// <summary><see cref="Backend.VoiceCount"/>.</summary>
	CountVoices = 1UL << 17,
	/// <summary><see cref="Backend.GetVoiceName"/>.</summary>
	GetVoiceName = 1UL << 18,
	/// <summary><see cref="Backend.GetVoiceLanguage"/>.</summary>
	GetVoiceLanguage = 1UL << 19,
	/// <summary>Getting <see cref="Backend.VoiceIndex"/>.</summary>
	GetVoice = 1UL << 20,
	/// <summary>Setting <see cref="Backend.VoiceIndex"/>.</summary>
	SetVoice = 1UL << 21,
	/// <summary><see cref="Backend.Channels"/>.</summary>
	GetChannels = 1UL << 22,
	/// <summary><see cref="Backend.SampleRate"/>.</summary>
	GetSampleRate = 1UL << 23,
	/// <summary><see cref="Backend.BitDepth"/>.</summary>
	GetBitDepth = 1UL << 24,
	/// <summary>Reserved by prism.</summary>
	SilenceTrimmingOnSpeak = 1UL << 25,
	/// <summary>Leading and trailing silence is trimmed before audio reaches the callback.</summary>
	SilenceTrimmingOnSpeakToMemory = 1UL << 26,
	/// <summary>Reserved by prism.</summary>
	SpeakSsml = 1UL << 27,
	/// <summary>Reserved by prism.</summary>
	SpeakToMemorySsml = 1UL << 28,
	/// <summary>Output goes through a screen reader. Never set together with <see cref="IsSpeechEngine"/>.</summary>
	IsScreenReader = 1UL << 56,
	/// <summary>A standalone speech engine. Never set together with <see cref="IsScreenReader"/>.</summary>
	IsSpeechEngine = 1UL << 57,
	/// <summary>
	/// Skipped by <see cref="Prism.CreateBest"/> and <see cref="Prism.AcquireBest"/>, so it is only
	/// used when created or acquired by id.
	/// </summary>
	RequiresExplicitSelection = 1UL << 58
}
