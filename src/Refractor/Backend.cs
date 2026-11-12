using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>Receives synthesized audio: interleaved samples in [-1, 1], valid only during the call.</summary>
public delegate void AudioCallback(ReadOnlySpan<float> samples, int channels, int sampleRate);

/// <summary>A voice a backend offers. <see cref="Index"/> can change after <see cref="Backend.RefreshVoices"/>.</summary>
public sealed record Voice(int Index, string Name, string? Language);

/// <summary>
/// A speech, braille or screen reader output. Not thread safe: use one instance from one thread
/// at a time. Check <see cref="GetFeatures"/> before relying on an operation, since an
/// unsupported one throws <see cref="PrismException"/> with <see cref="PrismError.NotImplemented"/>.
/// </summary>
public sealed unsafe class Backend : IDisposable {
	private readonly BackendHandle _handle;
	private string? _name;

	internal Backend(PrismBackend* backend) => _handle = new BackendHandle(backend);

	public string Name {
		get {
			if (_name is not null) return _name;
			using Lease lease = Borrow();
			return _name = Utf8.Decode(PrismNative.BackendName(Native(lease))) ?? string.Empty;
		}
	}

	/// <summary>May probe the system to fill in <see cref="BackendFeatures.IsSupportedAtRuntime"/>, so cache the result.</summary>
	public BackendFeatures GetFeatures() {
		using Lease lease = Borrow();
		return (BackendFeatures)PrismNative.BackendGetFeatures(Native(lease));
	}

	public bool Supports(BackendFeatures features) => (GetFeatures() & features) == features;

	/// <summary>Returns false when it was already initialized, which is not an error.</summary>
	public bool Initialize() {
		using Lease lease = Borrow();
		PrismError error = PrismNative.BackendInitialize(Native(lease));
		if (error == PrismError.AlreadyInitialized) return false;
		PrismException.ThrowIfFailed(error);
		return true;
	}

	/// <summary>Speaks, and may return before the speech ends. Without interrupting, most backends queue.</summary>
	public void Speak(string text, bool interrupt = false) {
		using Lease lease = Borrow();
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendSpeak(Native(lease), native, interrupt));
	}

	/// <summary>
	/// Synthesizes to <paramref name="callback"/> instead of the speakers, and returns only when
	/// all audio is delivered. The callback may run on another thread and must not use this
	/// backend. An exception it throws is rethrown here once synthesis ends.
	/// </summary>
	public void SpeakToMemory(string text, AudioCallback callback) {
		ArgumentNullException.ThrowIfNull(callback);
		AudioDelivery delivery = new(callback);
		GCHandle handle = GCHandle.Alloc(delivery);
		try {
			using Lease lease = Borrow();
			fixed (byte* native = Utf8.Encode(text, nameof(text))) {
				PrismError error = PrismNative.BackendSpeakToMemory(Native(lease), native, &DeliverAudio, (void*)GCHandle.ToIntPtr(handle));
				delivery.Failure?.Throw();
				PrismException.ThrowIfFailed(error);
			}
		} finally {
			handle.Free();
		}
	}

	public void Braille(string text) {
		using Lease lease = Borrow();
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendBraille(Native(lease), native));
	}

	/// <summary>Speech and braille together. Success can mean only one of them worked.</summary>
	public void Output(string text, bool interrupt = false) {
		using Lease lease = Borrow();
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendOutput(Native(lease), native, interrupt));
	}

	/// <summary>Stops this backend's speech and drops its queue. A screen reader's own speech carries on.</summary>
	public void Stop() {
		using Lease lease = Borrow();
		PrismException.ThrowIfFailed(PrismNative.BackendStop(Native(lease)));
	}

	public void Pause() {
		using Lease lease = Borrow();
		PrismException.ThrowIfFailed(PrismNative.BackendPause(Native(lease)));
	}

	public void Resume() {
		using Lease lease = Borrow();
		PrismException.ThrowIfFailed(PrismNative.BackendResume(Native(lease)));
	}

	public bool IsSpeaking {
		get {
			using Lease lease = Borrow();
			byte speaking;
			PrismException.ThrowIfFailed(PrismNative.BackendIsSpeaking(Native(lease), &speaking));
			return speaking != 0;
		}
	}

	/// <summary>From 0 to 1.</summary>
	public float Volume {
		get {
			using Lease lease = Borrow();
			float volume;
			PrismException.ThrowIfFailed(PrismNative.BackendGetVolume(Native(lease), &volume));
			return volume;
		}
		set {
			using Lease lease = Borrow();
			PrismException.ThrowIfFailed(PrismNative.BackendSetVolume(Native(lease), value));
		}
	}

	/// <summary>From 0 to 1, where 0.5 is always the backend's default.</summary>
	public float Rate {
		get {
			using Lease lease = Borrow();
			float rate;
			PrismException.ThrowIfFailed(PrismNative.BackendGetRate(Native(lease), &rate));
			return rate;
		}
		set {
			using Lease lease = Borrow();
			PrismException.ThrowIfFailed(PrismNative.BackendSetRate(Native(lease), value));
		}
	}

	/// <summary>From 0 to 1. Screen readers do not support it.</summary>
	public float Pitch {
		get {
			using Lease lease = Borrow();
			float pitch;
			PrismException.ThrowIfFailed(PrismNative.BackendGetPitch(Native(lease), &pitch));
			return pitch;
		}
		set {
			using Lease lease = Borrow();
			PrismException.ThrowIfFailed(PrismNative.BackendSetPitch(Native(lease), value));
		}
	}

	/// <summary>Picks up newly installed voices. Indexes may move, so store voice names, not indexes.</summary>
	public void RefreshVoices() {
		using Lease lease = Borrow();
		PrismException.ThrowIfFailed(PrismNative.BackendRefreshVoices(Native(lease)));
	}

	public int VoiceCount {
		get {
			using Lease lease = Borrow();
			nuint count;
			PrismException.ThrowIfFailed(PrismNative.BackendCountVoices(Native(lease), &count));
			return checked((int)count);
		}
	}

	public string GetVoiceName(int index) {
		using Lease lease = Borrow();
		byte* name;
		PrismException.ThrowIfFailed(PrismNative.BackendGetVoiceName(Native(lease), Index(index), &name));
		return Utf8.Decode(name) ?? string.Empty;
	}

	/// <summary>The voice's language, such as "en-US", or null when the backend reports none for it.</summary>
	public string? GetVoiceLanguage(int index) {
		using Lease lease = Borrow();
		byte* language;
		PrismException.ThrowIfFailed(PrismNative.BackendGetVoiceLanguage(Native(lease), Index(index), &language));
		string? decoded = Utf8.Decode(language);
		return string.IsNullOrEmpty(decoded) ? null : decoded;
	}

	/// <summary>The index of the current voice.</summary>
	public int VoiceIndex {
		get {
			using Lease lease = Borrow();
			nuint index;
			PrismException.ThrowIfFailed(PrismNative.BackendGetVoice(Native(lease), &index));
			return checked((int)index);
		}
		set {
			using Lease lease = Borrow();
			PrismException.ThrowIfFailed(PrismNative.BackendSetVoice(Native(lease), Index(value)));
		}
	}

	/// <summary>Every voice, with its language when the backend reports one.</summary>
	public IReadOnlyList<Voice> GetVoices() {
		int count = VoiceCount;
		bool hasLanguages = Supports(BackendFeatures.GetVoiceLanguage);
		Voice[] voices = new Voice[count];
		for (int index = 0; index < count; index++) voices[index] = new Voice(index, GetVoiceName(index), hasLanguages ? GetVoiceLanguage(index) : null);
		return voices;
	}

	public int Channels {
		get {
			using Lease lease = Borrow();
			nuint channels;
			PrismException.ThrowIfFailed(PrismNative.BackendGetChannels(Native(lease), &channels));
			return checked((int)channels);
		}
	}

	public int SampleRate {
		get {
			using Lease lease = Borrow();
			nuint sampleRate;
			PrismException.ThrowIfFailed(PrismNative.BackendGetSampleRate(Native(lease), &sampleRate));
			return checked((int)sampleRate);
		}
	}

	/// <summary>The engine's native depth. <see cref="SpeakToMemory"/> always delivers 32 bit floats.</summary>
	public int BitDepth {
		get {
			using Lease lease = Borrow();
			nuint bitDepth;
			PrismException.ThrowIfFailed(PrismNative.BackendGetBitDepth(Native(lease), &bitDepth));
			return checked((int)bitDepth);
		}
	}

	/// <summary>Frees this handle. Speech in progress may or may not stop, so call <see cref="Stop"/> first to be sure.</summary>
	public void Dispose() => _handle.Dispose();

	internal static Backend Wrap(PrismBackend* backend) =>
		backend is not null ? new Backend(backend) : throw new PrismException(PrismError.BackendNotAvailable);

	private Lease Borrow() => new(_handle, nameof(Backend));

	private static PrismBackend* Native(in Lease lease) => (PrismBackend*)lease.Pointer;

	private static nuint Index(int index) {
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		return (nuint)index;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void DeliverAudio(void* userdata, float* samples, nuint sampleCount, nuint channels, nuint sampleRate) {
		AudioDelivery delivery = (AudioDelivery)GCHandle.FromIntPtr((nint)userdata).Target!;
		if (delivery.Failure is not null) return;
		try {
			ReadOnlySpan<float> chunk = samples is null ? [] : new ReadOnlySpan<float>(samples, checked((int)sampleCount));
			delivery.Callback(chunk, (int)channels, (int)sampleRate);
		} catch (Exception error) {
			delivery.Failure = ExceptionDispatchInfo.Capture(error);
		}
	}

	// An exception cannot cross back into prism, so the first one is held and rethrown on the
	// calling thread, and the rest of the audio is dropped.
	private sealed class AudioDelivery(AudioCallback callback) {
		public AudioCallback Callback { get; } = callback;

		public ExceptionDispatchInfo? Failure { get; set; }
	}
}
