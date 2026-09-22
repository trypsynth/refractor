using Refractor.Interop;

namespace Refractor;

/// <summary>
/// A backend written in .NET, registered with <see cref="RegistryBuilder.AddBackend"/>. Override
/// exactly the operations named in the features passed there; prism never calls the rest.
/// Calls for one instance never overlap, but can arrive on any thread. Throw
/// <see cref="PrismException"/> to report a specific error; any other exception becomes
/// <see cref="PrismError.Internal"/> and is written to prism's log. If the backend is
/// <see cref="IDisposable"/>, it is disposed when prism destroys the instance.
/// </summary>
public abstract class CustomBackend {
	/// <summary>Whether the engine can be used right now. Can run before <see cref="Initialize"/>, on the poll thread.</summary>
	public virtual bool IsSupported() => true;

	public virtual void Initialize() { }

	public virtual void Speak(string text, bool interrupt) => throw NotImplemented();

	/// <summary>Deliver every sample through <paramref name="sink"/> before returning.</summary>
	public virtual void SpeakToMemory(string text, AudioSink sink) => throw NotImplemented();

	public virtual void Braille(string text) => throw NotImplemented();

	public virtual void Output(string text, bool interrupt) => throw NotImplemented();

	public virtual void Stop() => throw NotImplemented();

	public virtual void Pause() => throw NotImplemented();

	public virtual void Resume() => throw NotImplemented();

	public virtual bool IsSpeaking() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetVolume(float volume) => throw NotImplemented();

	public virtual float GetVolume() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetRate(float rate) => throw NotImplemented();

	public virtual float GetRate() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetPitch(float pitch) => throw NotImplemented();

	public virtual float GetPitch() => throw NotImplemented();

	public virtual void RefreshVoices() => throw NotImplemented();

	public virtual int CountVoices() => throw NotImplemented();

	public virtual string GetVoiceName(int index) => throw NotImplemented();

	public virtual string GetVoiceLanguage(int index) => throw NotImplemented();

	public virtual void SetVoice(int index) => throw NotImplemented();

	public virtual int GetVoice() => throw NotImplemented();

	public virtual int GetChannels() => throw NotImplemented();

	public virtual int GetSampleRate() => throw NotImplemented();

	public virtual int GetBitDepth() => throw NotImplemented();

	private static PrismException NotImplemented() => new(PrismError.NotImplemented);
}

/// <summary>Where <see cref="CustomBackend.SpeakToMemory"/> sends audio. It only exists for that call.</summary>
public readonly unsafe ref struct AudioSink {
	private readonly delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> _callback;
	private readonly void* _userdata;

	internal AudioSink(delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata) {
		_callback = callback;
		_userdata = userdata;
	}

	/// <summary>Sends interleaved samples in [-1, 1].</summary>
	public void Write(ReadOnlySpan<float> samples, int channels, int sampleRate) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
		if (_callback is null) return;
		fixed (float* data = samples) _callback(_userdata, data, (nuint)samples.Length, (nuint)channels, (nuint)sampleRate);
	}
}
