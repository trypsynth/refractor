namespace Refractor;

/// <summary>A prism error code, carried by <see cref="PrismException.Error"/>. The numbers are part of prism's ABI and never change.</summary>
public enum PrismError {
	/// <summary>Success. Refractor never throws with it.</summary>
	Ok = 0,
	/// <summary>The backend has not been initialized. Call <see cref="Backend.Initialize"/> first.</summary>
	NotInitialized,
	/// <summary>An argument was not valid.</summary>
	InvalidParameter,
	/// <summary>The backend does not support the operation. <see cref="Backend.GetFeatures"/> says what it does support.</summary>
	NotImplemented,
	/// <summary>The backend has no voices.</summary>
	NoVoices,
	/// <summary>No voice has the index given.</summary>
	VoiceNotFound,
	/// <summary>The engine failed to speak.</summary>
	SpeakFailure,
	/// <summary>Memory could not be allocated.</summary>
	MemoryFailure,
	/// <summary>A value was outside its range, such as a volume, rate or pitch outside [0, 1].</summary>
	RangeOutOfBounds,
	/// <summary>An unexpected error inside prism or the backend. A custom backend that throws an unrecognized exception reports this.</summary>
	Internal,
	/// <summary>The operation needs speech in progress, and there is none.</summary>
	NotSpeaking,
	/// <summary>The operation needs paused speech, and speech is not paused.</summary>
	NotPaused,
	/// <summary>Speech is already paused.</summary>
	AlreadyPaused,
	/// <summary>Text was not valid UTF-8. Refractor's own strings always are.</summary>
	InvalidUtf8,
	/// <summary>The operation is not valid in the backend's current state.</summary>
	InvalidOperation,
	/// <summary>The backend was already initialized. <see cref="Backend.Initialize"/> returns false for it rather than throwing.</summary>
	AlreadyInitialized,
	/// <summary>The backend's engine or screen reader is not available, such as a screen reader that is not running.</summary>
	BackendNotAvailable,
	/// <summary>An error prism could not identify.</summary>
	Unknown,
	/// <summary>The engine produced audio in a format prism cannot use.</summary>
	InvalidAudioFormat,
	/// <summary>The backend allows only so many instances at once, and creating another would pass that limit.</summary>
	InternalBackendLimitExceeded,
	/// <summary>The backend got into a state it cannot recover from, and should be disposed.</summary>
	BackendEnteredUndefinedState,
	/// <summary>A plugin library could not be loaded.</summary>
	LibraryLoadFailed,
	/// <summary>A library loaded, but is not a prism plugin.</summary>
	LibraryInvalid,
	/// <summary>A plugin library was built for a different version of prism's plugin ABI.</summary>
	IncompatibleAbi
}
