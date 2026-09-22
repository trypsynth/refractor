namespace Refractor.Tests;

public class CustomBackendTests {
	private const string Name = "Recording";
	private const int HighestPriority = 1_000_000;

	[Fact]
	public void RegistersUnderTheNameGiven() {
		using Harness harness = new();
		Assert.Equal(harness.Id, harness.Prism.FindBackend(Name));
		Assert.Equal(Name, harness.Prism.GetBackendName(harness.Id));
		Assert.Equal(HighestPriority, harness.Prism.GetBackendPriority(harness.Id));
		Assert.Contains(BackendId.Sapi, harness.Prism.BackendIds);
	}

	[Fact]
	public void ANewInstanceMustBeInitializedFirst() {
		using Harness harness = new();
		using Backend backend = harness.Prism.Create(harness.Id);
		PrismException error = Assert.Throws<PrismException>(() => backend.Speak("Too soon"));
		Assert.Equal(PrismError.NotInitialized, error.Error);
		Assert.True(backend.Initialize());
		Assert.False(backend.Initialize());
		Assert.True(harness.Last.Initialized);
	}

	[Fact]
	public void SpeechReachesTheBackendIntact() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		backend.Speak("Café, naïve, 😀");
		backend.Speak("Now", interrupt: true);
		Assert.Equal([("Café, naïve, 😀", false), ("Now", true)], harness.Last.Spoken);
	}

	[Fact]
	public void TheBestBackendIsTheHighestPriorityThatStarts() {
		using Harness harness = new();
		using Backend backend = harness.Prism.CreateBest();
		Assert.Equal(Name, backend.Name);
	}

	[Fact]
	public void ReportsDeclaredFeaturesAndLiveAvailability() {
		using Harness harness = new();
		using Backend backend = harness.Prism.Create(harness.Id);
		Assert.Equal(RecordingBackend.Features | BackendFeatures.IsSupportedAtRuntime, backend.GetFeatures());
		Assert.True(backend.Supports(BackendFeatures.Speak | BackendFeatures.Braille));
	}

	[Fact]
	public void AnUndeclaredOperationIsNotImplemented() {
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend("SpeechOnly", 1, BackendFeatures.Speak, () => new RecordingBackend());
		using Registry registry = builder.Freeze();
		using Prism prism = new(new PrismOptions { Registry = registry });
		using Backend backend = prism.Create(id);
		backend.Initialize();
		Assert.Equal(BackendFeatures.Speak | BackendFeatures.IsSupportedAtRuntime, backend.GetFeatures());
		Assert.Equal(PrismError.NotImplemented, Assert.Throws<PrismException>(backend.Pause).Error);
		Assert.Equal(PrismError.NotImplemented, Assert.Throws<PrismException>(() => backend.Rate).Error);
		Assert.Equal(PrismError.NotImplemented, Assert.Throws<PrismException>(() => backend.Braille("Hello")).Error);
		Assert.Equal(PrismError.NotImplemented, Assert.Throws<PrismException>(() => backend.GetVoices()).Error);
	}

	[Fact]
	public void BraillesAndOutputsBothWays() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		backend.Braille("Dots");
		backend.Output("Both", interrupt: true);
		Assert.Equal(["Dots"], harness.Last.Brailled);
		Assert.Equal([("Both", true)], harness.Last.Outputs);
	}

	[Fact]
	public void PausesAndResumes() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		backend.Pause();
		Assert.True(harness.Last.Paused);
		backend.Resume();
		Assert.False(harness.Last.Paused);
	}

	[Fact]
	public void RateAndPitchRoundTrip() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		backend.Rate = 0.9f;
		backend.Pitch = 0.2f;
		Assert.Equal(0.9f, backend.Rate);
		Assert.Equal(0.2f, backend.Pitch);
		Assert.Equal(PrismError.RangeOutOfBounds, Assert.Throws<PrismException>(() => backend.Rate = -0.1f).Error);
	}

	[Fact]
	public void RefreshesVoices() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		backend.RefreshVoices();
		Assert.Equal(1, harness.Last.Refreshes);
		Assert.Equal(3, backend.VoiceCount);
	}

	[Fact]
	public void APrismErrorFromTheBackendComesBackUnchanged() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		harness.Last.Failure = new PrismException(PrismError.SpeakFailure);
		Assert.Equal(PrismError.SpeakFailure, Assert.Throws<PrismException>(() => backend.Speak("Hello")).Error);
	}

	[Fact]
	public void AnyOtherExceptionBecomesAnInternalError() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		harness.Last.Failure = new InvalidOperationException("Engine fell over");
		Assert.Equal(PrismError.Internal, Assert.Throws<PrismException>(() => backend.Speak("Hello")).Error);
	}

	[Fact]
	public void StopsAndReportsSpeakingState() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		Assert.False(backend.IsSpeaking);
		backend.Speak("Hello");
		Assert.True(backend.IsSpeaking);
		backend.Stop();
		Assert.False(backend.IsSpeaking);
	}

	[Fact]
	public void VolumeRoundTripsAndPrismGuardsItsRange() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		Assert.Equal(0.5f, backend.Volume);
		backend.Volume = 0.8f;
		Assert.Equal(0.8f, backend.Volume);
		Assert.Equal(PrismError.RangeOutOfBounds, Assert.Throws<PrismException>(() => backend.Volume = 1.5f).Error);
		Assert.Equal(PrismError.RangeOutOfBounds, Assert.Throws<PrismException>(() => backend.Volume = float.NaN).Error);
		Assert.Equal(0.8f, backend.Volume);
	}

	[Fact]
	public void ListsVoicesWithTheirLanguages() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		IReadOnlyList<Voice> voices = backend.GetVoices();
		Assert.Equal([new Voice(0, "Alpha", "en-US"), new Voice(1, "Bravo", "fr-FR"), new Voice(2, "Charlie", "de-DE")], voices);
		Assert.Equal("Charlie", backend.GetVoiceName(2));
		Assert.Equal("Alpha", backend.GetVoiceName(0));
	}

	[Fact]
	public void SwitchesVoices() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		Assert.Equal(0, backend.VoiceIndex);
		backend.VoiceIndex = 2;
		Assert.Equal(2, backend.VoiceIndex);
		Assert.Equal(PrismError.VoiceNotFound, Assert.Throws<PrismException>(() => backend.VoiceIndex = 9).Error);
		Assert.Throws<ArgumentOutOfRangeException>(() => backend.VoiceIndex = -1);
	}

	[Fact]
	public void ReportsTheAudioFormat() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		Assert.Equal(2, backend.Channels);
		Assert.Equal(22050, backend.SampleRate);
		Assert.Equal(16, backend.BitDepth);
	}

	[Fact]
	public void SynthesizesToMemoryInChunks() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		List<float> samples = [];
		List<(int Channels, int SampleRate)> formats = [];
		backend.SpeakToMemory("Hello", (chunk, channels, sampleRate) => {
			samples.AddRange(chunk.ToArray());
			formats.Add((channels, sampleRate));
		});
		Assert.Equal([0.25f, -0.25f, 0.5f, -0.5f, 1f, -1f], samples);
		Assert.All(formats, format => Assert.Equal((2, 22050), format));
	}

	[Fact]
	public void AnExceptionFromTheAudioCallbackIsRethrown() {
		using Harness harness = new();
		using Backend backend = harness.CreateInitialized();
		int calls = 0;
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => backend.SpeakToMemory("Hello", (_, _, _) => {
			calls++;
			throw new InvalidOperationException("Disk full");
		}));
		Assert.Equal("Disk full", error.Message);
		Assert.Equal(1, calls);
	}

	[Fact]
	public void EveryCreatedInstanceGetsItsOwnBackend() {
		using Harness harness = new();
		using Backend first = harness.CreateInitialized();
		using Backend second = harness.CreateInitialized();
		first.Volume = 0.1f;
		Assert.Equal(0.5f, second.Volume);
		Assert.Equal(2, harness.Created.Count);
	}

	[Fact]
	public void AcquiredInstancesShareOneBackend() {
		using Harness harness = new();
		using Backend first = harness.Prism.Acquire(harness.Id);
		first.Initialize();
		using Backend second = harness.Prism.Acquire(harness.Id);
		Assert.False(second.Initialize());
		first.Volume = 0.1f;
		Assert.Equal(0.1f, second.Volume);
		using Backend cached = harness.Prism.GetCachedBackend(harness.Id)!;
		Assert.Equal(0.1f, cached.Volume);
		Assert.Single(harness.Created);
	}

	[Fact]
	public void FreeingTheLastHandleDisposesTheBackend() {
		using Harness harness = new();
		Backend backend = harness.CreateInitialized();
		RecordingBackend recording = harness.Last;
		Assert.False(recording.Disposed);
		backend.Dispose();
		Assert.True(recording.Disposed);
		Assert.Throws<ObjectDisposedException>(() => backend.Name);
	}

	[Fact]
	public void AFactoryReturningNullFailsTheCreation() {
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend("Refuses", 1, BackendFeatures.Speak, () => null);
		using Registry registry = builder.Freeze();
		using Prism prism = new(new PrismOptions { Registry = registry });
		Assert.Equal(PrismError.BackendNotAvailable, Assert.Throws<PrismException>(() => prism.Create(id)).Error);
	}

	[Fact]
	public void TheRegistryCanBeDisposedOnceBound() {
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend(Name, 1, RecordingBackend.Features, () => new RecordingBackend());
		Prism prism;
		using (Registry registry = builder.Freeze()) {
			prism = new Prism(new PrismOptions { Registry = registry });
		}
		using (prism) {
			using Backend backend = prism.Create(id);
			Assert.True(backend.Initialize());
		}
	}

	[Fact]
	public void ARetainedRegistryOutlivesTheOriginal() {
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend(Name, 1, RecordingBackend.Features, () => new RecordingBackend());
		Registry original = builder.Freeze();
		using Registry retained = original.Retain();
		original.Dispose();
		using Prism prism = new(new PrismOptions { Registry = retained });
		Assert.True(prism.HasBackend(id));
	}

	[Fact]
	public void RefusesADuplicateOrBuiltInName() {
		using RegistryBuilder builder = new();
		builder.AddBackend(Name, 1, BackendFeatures.Speak, () => new RecordingBackend());
		Assert.Equal(PrismError.InvalidOperation, Assert.Throws<PrismException>(() => builder.AddBackend(Name, 1, BackendFeatures.Speak, () => new RecordingBackend())).Error);
		Assert.Equal(PrismError.InvalidOperation, Assert.Throws<PrismException>(() => builder.AddBackend("NVDA", 1, BackendFeatures.Speak, () => new RecordingBackend())).Error);
	}

	[Fact]
	public void RefusesANegativePriority() {
		using RegistryBuilder builder = new();
		Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddBackend(Name, -1, BackendFeatures.Speak, () => new RecordingBackend()));
	}

	[Fact]
	public void ABuilderFreezesOnce() {
		using RegistryBuilder builder = new();
		using Registry registry = builder.Freeze();
		Assert.Throws<InvalidOperationException>(builder.Freeze);
	}

	[Fact]
	public void AMissingPluginFailsToLoad() {
		using RegistryBuilder builder = new();
		PrismException error = Assert.Throws<PrismException>(() => builder.AddLibrary(Path.Combine(AppContext.BaseDirectory, "no-such-plugin.dll")));
		Assert.Equal(PrismError.LibraryLoadFailed, error.Error);
	}

	private sealed class Harness : IDisposable {
		private readonly Registry _registry;

		public Harness() {
			using RegistryBuilder builder = new();
			Id = builder.AddBackend(Name, HighestPriority, RecordingBackend.Features, () => {
				RecordingBackend backend = new();
				lock (Created) Created.Add(backend);
				return backend;
			});
			_registry = builder.Freeze();
			Prism = new Prism(new PrismOptions { Registry = _registry });
		}

		public BackendId Id { get; }

		public Prism Prism { get; }

		public List<RecordingBackend> Created { get; } = [];

		public RecordingBackend Last => Created[^1];

		public Backend CreateInitialized() {
			Backend backend = Prism.Create(Id);
			backend.Initialize();
			return backend;
		}

		public void Dispose() {
			Prism.Dispose();
			_registry.Dispose();
		}
	}
}
