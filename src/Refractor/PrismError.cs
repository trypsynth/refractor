namespace Refractor;

/// <summary>A prism error code. The numbers are part of prism's ABI and never change.</summary>
public enum PrismError {
	Ok = 0,
	NotInitialized,
	InvalidParameter,
	NotImplemented,
	NoVoices,
	VoiceNotFound,
	SpeakFailure,
	MemoryFailure,
	RangeOutOfBounds,
	Internal,
	NotSpeaking,
	NotPaused,
	AlreadyPaused,
	InvalidUtf8,
	InvalidOperation,
	AlreadyInitialized,
	BackendNotAvailable,
	Unknown,
	InvalidAudioFormat,
	InternalBackendLimitExceeded,
	BackendEnteredUndefinedState,
	LibraryLoadFailed,
	LibraryInvalid,
	IncompatibleAbi
}
