namespace Refractor.Tests;

// prism's No Speech backend implements every operation without making a sound, so it drives the
// whole native path on any machine, speech engine or not.
public class NoSpeechTests {
	[Fact]
	public void IsOnlyUsedWhenAskedFor() {
		// A machine with no speech engine running, like a CI runner, would otherwise have no best
		// backend at all, so one that always starts is registered at No Speech's own priority.
		using RegistryBuilder builder = new();
		builder.AddBackend("Fallback", 0, BackendFeatures.Speak, () => new RecordingBackend());
		using Registry registry = builder.Freeze();
		using Prism prism = new(new PrismOptions { Registry = registry });
		Assert.True(prism.HasBackend(BackendId.NoSpeech));
		using Backend backend = prism.Create(BackendId.NoSpeech);
		Assert.True(backend.Supports(BackendFeatures.RequiresExplicitSelection));
		using Backend best = prism.CreateBest();
		Assert.NotEqual("No Speech", best.Name);
	}

	[Fact]
	public void AcceptsEveryOperation() {
		using Prism prism = new();
		using Backend backend = prism.Create(BackendId.NoSpeech);
		Assert.True(backend.Initialize());
		backend.Speak("Hello");
		backend.Output("Hello", interrupt: true);
		backend.Braille("Hello");
		backend.Pause();
		backend.Resume();
		backend.Stop();
		Assert.False(backend.IsSpeaking);
		backend.Rate = 0.75f;
		Assert.Equal(0.75f, backend.Rate);
		Assert.Equal([new Voice(0, "No Speech", "und")], backend.GetVoices());
	}

	[Fact]
	public void SynthesizesSilence() {
		using Prism prism = new();
		using Backend backend = prism.Create(BackendId.NoSpeech);
		backend.Initialize();
		int samples = 0;
		backend.SpeakToMemory("Hello", (chunk, channels, sampleRate) => {
			Assert.Equal(backend.Channels, channels);
			Assert.Equal(backend.SampleRate, sampleRate);
			Assert.All(chunk.ToArray(), sample => Assert.Equal(0f, sample));
			samples += chunk.Length;
		});
		Assert.True(samples > 0);
	}
}
