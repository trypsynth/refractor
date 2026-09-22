namespace Refractor;

/// <summary>What a backend implements, plus whether its engine is running right now.</summary>
[Flags]
public enum BackendFeatures : ulong {
	None = 0,
	/// <summary>The engine or screen reader is available. Advisory: initializing can still fail.</summary>
	IsSupportedAtRuntime = 1UL << 0,
	Speak = 1UL << 2,
	SpeakToMemory = 1UL << 3,
	Braille = 1UL << 4,
	Output = 1UL << 5,
	IsSpeaking = 1UL << 6,
	Stop = 1UL << 7,
	Pause = 1UL << 8,
	Resume = 1UL << 9,
	SetVolume = 1UL << 10,
	GetVolume = 1UL << 11,
	SetRate = 1UL << 12,
	GetRate = 1UL << 13,
	SetPitch = 1UL << 14,
	GetPitch = 1UL << 15,
	RefreshVoices = 1UL << 16,
	CountVoices = 1UL << 17,
	GetVoiceName = 1UL << 18,
	GetVoiceLanguage = 1UL << 19,
	GetVoice = 1UL << 20,
	SetVoice = 1UL << 21,
	GetChannels = 1UL << 22,
	GetSampleRate = 1UL << 23,
	GetBitDepth = 1UL << 24,
	/// <summary>Reserved by prism.</summary>
	SilenceTrimmingOnSpeak = 1UL << 25,
	/// <summary>Leading and trailing silence is trimmed before audio reaches the callback.</summary>
	SilenceTrimmingOnSpeakToMemory = 1UL << 26,
	/// <summary>Reserved by prism.</summary>
	SpeakSsml = 1UL << 27,
	/// <summary>Reserved by prism.</summary>
	SpeakToMemorySsml = 1UL << 28
}
