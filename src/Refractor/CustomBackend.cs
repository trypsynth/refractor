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

	/// <summary>Starts the engine. Runs once, before any other operation but <see cref="IsSupported"/>.</summary>
	public virtual void Initialize() { }

	/// <summary>Speaks <paramref name="text"/>, after stopping current speech when <paramref name="interrupt"/> is true and otherwise after it. <see cref="BackendFeatures.Speak"/>.</summary>
	public virtual void Speak(string text, bool interrupt) => throw NotImplemented();

	/// <summary>Synthesizes <paramref name="text"/> and delivers every sample through <paramref name="sink"/> before returning. <see cref="BackendFeatures.SpeakToMemory"/>.</summary>
	public virtual void SpeakToMemory(string text, AudioSink sink) => throw NotImplemented();

	/// <summary>Shows <paramref name="text"/> on a braille display. <see cref="BackendFeatures.Braille"/>.</summary>
	public virtual void Braille(string text) => throw NotImplemented();

	/// <summary>Speaks and brailles <paramref name="text"/>. <see cref="BackendFeatures.Output"/>.</summary>
	public virtual void Output(string text, bool interrupt) => throw NotImplemented();

	/// <summary>Stops speech and drops anything queued. <see cref="BackendFeatures.Stop"/>.</summary>
	public virtual void Stop() => throw NotImplemented();

	/// <summary>Pauses speech where it is. <see cref="BackendFeatures.Pause"/>.</summary>
	public virtual void Pause() => throw NotImplemented();

	/// <summary>Carries on from <see cref="Pause"/>. <see cref="BackendFeatures.Resume"/>.</summary>
	public virtual void Resume() => throw NotImplemented();

	/// <summary>Whether speech is in progress. <see cref="BackendFeatures.IsSpeaking"/>.</summary>
	public virtual bool IsSpeaking() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetVolume(float volume) => throw NotImplemented();

	/// <summary>The volume, from 0 to 1. <see cref="BackendFeatures.GetVolume"/>.</summary>
	public virtual float GetVolume() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetRate(float rate) => throw NotImplemented();

	/// <summary>The rate, from 0 to 1, where 0.5 is the engine's default. <see cref="BackendFeatures.GetRate"/>.</summary>
	public virtual float GetRate() => throw NotImplemented();

	/// <summary>Already checked by prism to be finite and within [0, 1].</summary>
	public virtual void SetPitch(float pitch) => throw NotImplemented();

	/// <summary>The pitch, from 0 to 1. <see cref="BackendFeatures.GetPitch"/>.</summary>
	public virtual float GetPitch() => throw NotImplemented();

	/// <summary>Picks up voices installed since the last refresh. <see cref="BackendFeatures.RefreshVoices"/>.</summary>
	public virtual void RefreshVoices() => throw NotImplemented();

	/// <summary>How many voices there are. <see cref="BackendFeatures.CountVoices"/>.</summary>
	public virtual int CountVoices() => throw NotImplemented();

	/// <summary>The voice's name. An index out of range should throw <see cref="ArgumentOutOfRangeException"/>. <see cref="BackendFeatures.GetVoiceName"/>.</summary>
	public virtual string GetVoiceName(int index) => throw NotImplemented();

	/// <summary>The voice's language, such as "en-US". <see cref="BackendFeatures.GetVoiceLanguage"/>.</summary>
	public virtual string GetVoiceLanguage(int index) => throw NotImplemented();

	/// <summary>Switches to the voice at <paramref name="index"/>. <see cref="BackendFeatures.SetVoice"/>.</summary>
	public virtual void SetVoice(int index) => throw NotImplemented();

	/// <summary>The index of the current voice. <see cref="BackendFeatures.GetVoice"/>.</summary>
	public virtual int GetVoice() => throw NotImplemented();

	/// <summary>How many channels <see cref="SpeakToMemory"/> produces. <see cref="BackendFeatures.GetChannels"/>.</summary>
	public virtual int GetChannels() => throw NotImplemented();

	/// <summary>The sample rate <see cref="SpeakToMemory"/> produces, in hertz. <see cref="BackendFeatures.GetSampleRate"/>.</summary>
	public virtual int GetSampleRate() => throw NotImplemented();

	/// <summary>The engine's native bit depth. <see cref="BackendFeatures.GetBitDepth"/>.</summary>
	public virtual int GetBitDepth() => throw NotImplemented();

	private static PrismException NotImplemented() => new(PrismError.NotImplemented);
}

/// <summary>
/// Where <see cref="CustomBackend.SpeakToMemory"/> sends audio. It only works during that call:
/// prism's receiver is gone once it returns, so a later <see cref="Write(ReadOnlySpan{float}, int, int)"/>
/// throws <see cref="ObjectDisposedException"/>.
/// </summary>
// A class rather than a ref struct, which would enforce that at compile time, because Visual
// Basic cannot use ref structs at all, and could not override SpeakToMemory.
public sealed unsafe class AudioSink {
	private readonly delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> _callback;
	private readonly void* _userdata;
	private volatile bool _closed;

	internal AudioSink(delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata) {
		_callback = callback;
		_userdata = userdata;
	}

	/// <summary>Sends interleaved samples in [-1, 1].</summary>
	public void Write(ReadOnlySpan<float> samples, int channels, int sampleRate) {
		ObjectDisposedException.ThrowIf(_closed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
		if (_callback is null) return;
		fixed (float* data = samples) _callback(_userdata, data, (nuint)samples.Length, (nuint)channels, (nuint)sampleRate);
	}

	/// <summary>Sends interleaved samples in [-1, 1].</summary>
	public void Write(float[] samples, int channels, int sampleRate) {
		ArgumentNullException.ThrowIfNull(samples);
		Write(samples.AsSpan(), channels, sampleRate);
	}

	internal void Close() => _closed = true;
}
