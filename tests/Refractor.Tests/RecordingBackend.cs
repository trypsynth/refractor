namespace Refractor.Tests;

// Stands in for a speech engine, so the tests exercise prism end to end without making a sound.
internal sealed class RecordingBackend : CustomBackend, IDisposable {
	internal const BackendFeatures Features =
		BackendFeatures.Speak
		| BackendFeatures.SpeakToMemory
		| BackendFeatures.Braille
		| BackendFeatures.Output
		| BackendFeatures.Stop
		| BackendFeatures.Pause
		| BackendFeatures.Resume
		| BackendFeatures.IsSpeaking
		| BackendFeatures.SetVolume
		| BackendFeatures.GetVolume
		| BackendFeatures.SetRate
		| BackendFeatures.GetRate
		| BackendFeatures.SetPitch
		| BackendFeatures.GetPitch
		| BackendFeatures.RefreshVoices
		| BackendFeatures.CountVoices
		| BackendFeatures.GetVoiceName
		| BackendFeatures.GetVoiceLanguage
		| BackendFeatures.SetVoice
		| BackendFeatures.GetVoice
		| BackendFeatures.GetChannels
		| BackendFeatures.GetSampleRate
		| BackendFeatures.GetBitDepth;

	internal static readonly (string Name, string Language)[] Voices = [("Alpha", "en-US"), ("Bravo", "fr-FR"), ("Charlie", "de-DE")];

	private float _volume = 0.5f;
	private float _rate = 0.5f;
	private float _pitch = 0.5f;
	private int _voice;
	private bool _speaking;

	internal List<(string Text, bool Interrupt)> Spoken { get; } = [];

	internal List<string> Brailled { get; } = [];

	internal List<(string Text, bool Interrupt)> Outputs { get; } = [];

	internal bool Paused { get; private set; }

	internal int Refreshes { get; private set; }

	internal bool Initialized { get; private set; }

	internal bool Disposed { get; private set; }

	internal Exception? Failure { get; set; }

	public override void Initialize() => Initialized = true;

	public override void Speak(string text, bool interrupt) {
		if (Failure is not null) throw Failure;
		Spoken.Add((text, interrupt));
		_speaking = true;
	}

	public override void SpeakToMemory(string text, AudioSink sink) {
		sink.Write([0.25f, -0.25f], 2, 22050);
		sink.Write([0.5f, -0.5f, 1f, -1f], 2, 22050);
	}

	public override void Braille(string text) => Brailled.Add(text);

	public override void Output(string text, bool interrupt) => Outputs.Add((text, interrupt));

	public override void Stop() => _speaking = false;

	public override void Pause() => Paused = true;

	public override void Resume() => Paused = false;

	public override void SetRate(float rate) => _rate = rate;

	public override float GetRate() => _rate;

	public override void SetPitch(float pitch) => _pitch = pitch;

	public override float GetPitch() => _pitch;

	public override void RefreshVoices() => Refreshes++;

	public override bool IsSpeaking() => _speaking;

	public override void SetVolume(float volume) => _volume = volume;

	public override float GetVolume() => _volume;

	public override int CountVoices() => Voices.Length;

	public override string GetVoiceName(int index) => Voices[index].Name;

	public override string GetVoiceLanguage(int index) => Voices[index].Language;

	public override void SetVoice(int index) {
		if (index >= Voices.Length) throw new PrismException(PrismError.VoiceNotFound);
		_voice = index;
	}

	public override int GetVoice() => _voice;

	public override int GetChannels() => 2;

	public override int GetSampleRate() => 22050;

	public override int GetBitDepth() => 16;

	public void Dispose() => Disposed = true;
}
