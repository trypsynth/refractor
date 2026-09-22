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
	private PrismBackend* _backend;
	private string? _name;

	internal Backend(PrismBackend* backend) => _backend = backend;

	~Backend() => Release();

	public string Name {
		get {
			PrismBackend* backend = Pointer;
			return _name ??= Utf8.Decode(PrismNative.BackendName(backend)) ?? string.Empty;
		}
	}

	/// <summary>May probe the system to fill in <see cref="BackendFeatures.IsSupportedAtRuntime"/>, so cache the result.</summary>
	public BackendFeatures GetFeatures() => (BackendFeatures)PrismNative.BackendGetFeatures(Pointer);

	public bool Supports(BackendFeatures features) => (GetFeatures() & features) == features;

	/// <summary>Returns false when it was already initialized, which is not an error.</summary>
	public bool Initialize() {
		PrismError error = PrismNative.BackendInitialize(Pointer);
		if (error == PrismError.AlreadyInitialized) return false;
		PrismException.ThrowIfFailed(error);
		return true;
	}

	/// <summary>Speaks, and may return before the speech ends. Without interrupting, most backends queue.</summary>
	public void Speak(string text, bool interrupt = false) {
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendSpeak(Pointer, native, interrupt));
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
			fixed (byte* native = Utf8.Encode(text, nameof(text))) {
				PrismError error = PrismNative.BackendSpeakToMemory(Pointer, native, &DeliverAudio, (void*)GCHandle.ToIntPtr(handle));
				delivery.Failure?.Throw();
				PrismException.ThrowIfFailed(error);
			}
		} finally {
			handle.Free();
		}
	}

	public void Braille(string text) {
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendBraille(Pointer, native));
	}

	/// <summary>Speech and braille together. Success can mean only one of them worked.</summary>
	public void Output(string text, bool interrupt = false) {
		fixed (byte* native = Utf8.Encode(text, nameof(text))) PrismException.ThrowIfFailed(PrismNative.BackendOutput(Pointer, native, interrupt));
	}

	/// <summary>Stops this backend's speech and drops its queue. A screen reader's own speech carries on.</summary>
	public void Stop() => PrismException.ThrowIfFailed(PrismNative.BackendStop(Pointer));

	public void Pause() => PrismException.ThrowIfFailed(PrismNative.BackendPause(Pointer));

	public void Resume() => PrismException.ThrowIfFailed(PrismNative.BackendResume(Pointer));

	public bool IsSpeaking {
		get {
			byte speaking;
			PrismException.ThrowIfFailed(PrismNative.BackendIsSpeaking(Pointer, &speaking));
			return speaking != 0;
		}
	}

	/// <summary>From 0 to 1.</summary>
	public float Volume {
		get {
			float volume;
			PrismException.ThrowIfFailed(PrismNative.BackendGetVolume(Pointer, &volume));
			return volume;
		}
		set => PrismException.ThrowIfFailed(PrismNative.BackendSetVolume(Pointer, value));
	}

	/// <summary>From 0 to 1, where 0.5 is always the backend's default.</summary>
	public float Rate {
		get {
			float rate;
			PrismException.ThrowIfFailed(PrismNative.BackendGetRate(Pointer, &rate));
			return rate;
		}
		set => PrismException.ThrowIfFailed(PrismNative.BackendSetRate(Pointer, value));
	}

	/// <summary>From 0 to 1. Screen readers do not support it.</summary>
	public float Pitch {
		get {
			float pitch;
			PrismException.ThrowIfFailed(PrismNative.BackendGetPitch(Pointer, &pitch));
			return pitch;
		}
		set => PrismException.ThrowIfFailed(PrismNative.BackendSetPitch(Pointer, value));
	}

	/// <summary>Picks up newly installed voices. Indexes may move, so store voice names, not indexes.</summary>
	public void RefreshVoices() => PrismException.ThrowIfFailed(PrismNative.BackendRefreshVoices(Pointer));

	public int VoiceCount {
		get {
			nuint count;
			PrismException.ThrowIfFailed(PrismNative.BackendCountVoices(Pointer, &count));
			return checked((int)count);
		}
	}

	public string GetVoiceName(int index) {
		byte* name;
		PrismException.ThrowIfFailed(PrismNative.BackendGetVoiceName(Pointer, Index(index), &name));
		return Utf8.Decode(name) ?? string.Empty;
	}

	public string GetVoiceLanguage(int index) {
		byte* language;
		PrismException.ThrowIfFailed(PrismNative.BackendGetVoiceLanguage(Pointer, Index(index), &language));
		return Utf8.Decode(language) ?? string.Empty;
	}

	/// <summary>The index of the current voice.</summary>
	public int VoiceIndex {
		get {
			nuint index;
			PrismException.ThrowIfFailed(PrismNative.BackendGetVoice(Pointer, &index));
			return checked((int)index);
		}
		set => PrismException.ThrowIfFailed(PrismNative.BackendSetVoice(Pointer, Index(value)));
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
			nuint channels;
			PrismException.ThrowIfFailed(PrismNative.BackendGetChannels(Pointer, &channels));
			return checked((int)channels);
		}
	}

	public int SampleRate {
		get {
			nuint sampleRate;
			PrismException.ThrowIfFailed(PrismNative.BackendGetSampleRate(Pointer, &sampleRate));
			return checked((int)sampleRate);
		}
	}

	/// <summary>The engine's native depth. <see cref="SpeakToMemory"/> always delivers 32 bit floats.</summary>
	public int BitDepth {
		get {
			nuint bitDepth;
			PrismException.ThrowIfFailed(PrismNative.BackendGetBitDepth(Pointer, &bitDepth));
			return checked((int)bitDepth);
		}
	}

	/// <summary>Frees this handle. Speech in progress may or may not stop, so call <see cref="Stop"/> first to be sure.</summary>
	public void Dispose() {
		Release();
		GC.SuppressFinalize(this);
	}

	internal static Backend Wrap(PrismBackend* backend) =>
		backend is not null ? new Backend(backend) : throw new PrismException(PrismError.BackendNotAvailable);

	private PrismBackend* Pointer => _backend is not null ? _backend : throw new ObjectDisposedException(nameof(Backend));

	private void Release() {
		if (_backend is null) return;
		PrismNative.BackendFree(_backend);
		_backend = null;
	}

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
