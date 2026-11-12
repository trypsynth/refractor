using System.Collections.Concurrent;

namespace Refractor.Tests;

public class AvailabilityTests {
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	[Fact]
	public void ReportsABackendComingAndGoing() {
		ToggledBackend.Available = true;
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend("Toggled", 1, BackendFeatures.Speak, () => new ToggledBackend());
		using Registry registry = builder.Freeze();
		BlockingCollection<(BackendId Id, string Name, bool Available)> changes = [];
		using ManualResetEventSlim baseline = new();
		using Prism prism = new(new PrismOptions {
			Registry = registry,
			PollInterval = TimeSpan.FromMilliseconds(20),
			DebounceSamples = 1,
			AvailabilityChanged = (backend, name, available) => {
				if (backend == id) changes.Add((backend, name, available));
			},
			AvailabilityBaselineEstablished = baseline.Set
		});
		Assert.True(baseline.Wait(Patience), "The baseline scan never finished.");
		ToggledBackend.Available = false;
		Assert.True(changes.TryTake(out var gone, Patience), "Going away was never reported.");
		Assert.Equal((id, "Toggled", false), gone);
		ToggledBackend.Available = true;
		Assert.True(changes.TryTake(out var back, Patience), "Coming back was never reported.");
		Assert.Equal((id, "Toggled", true), back);
	}

	[Fact]
	public void KeepsPollingWhenHandlersThrow() {
		ToggledBackend.Available = true;
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend("Throwing", 1, BackendFeatures.Speak, () => new ToggledBackend());
		using Registry registry = builder.Freeze();
		BlockingCollection<bool> changes = [];
		using ManualResetEventSlim baseline = new();
		using Prism prism = new(new PrismOptions {
			Registry = registry,
			PollInterval = TimeSpan.FromMilliseconds(20),
			DebounceSamples = 1,
			AvailabilityChanged = (backend, _, available) => {
				if (backend != id) return;
				changes.Add(available);
				throw new InvalidOperationException("A failing availability handler.");
			},
			AvailabilityBaselineEstablished = () => {
				baseline.Set();
				throw new InvalidOperationException("A failing baseline handler.");
			}
		});
		Assert.True(baseline.Wait(Patience), "The baseline scan never finished.");
		ToggledBackend.Available = false;
		Assert.True(changes.TryTake(out bool gone, Patience), "Going away was never reported.");
		Assert.False(gone);
		ToggledBackend.Available = true;
		Assert.True(changes.TryTake(out bool back, Patience), "Coming back was never reported after a handler threw.");
		Assert.True(back);
	}

	[Fact]
	public void PollingCanBePausedAndResumed() {
		using Prism prism = new(new PrismOptions { AvailabilityChanged = (_, _, _) => { } });
		prism.PauseAvailabilityPolling();
		prism.ResumeAvailabilityPolling();
		Assert.True(prism.BackendCount > 0);
	}

	private sealed class ToggledBackend : CustomBackend {
		private static volatile bool _available;

		public static bool Available {
			get => _available;
			set => _available = value;
		}

		public override bool IsSupported() => Available;

		public override void Speak(string text, bool interrupt) { }
	}
}
