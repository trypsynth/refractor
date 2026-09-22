using System.Collections.Concurrent;

namespace Refractor.Tests;

// The logger is shared by the whole process, so these tests must not run alongside others.
[CollectionDefinition(nameof(PrismLogCollection), DisableParallelization = true)]
public class PrismLogCollection;

[Collection(nameof(PrismLogCollection))]
public class PrismLogTests : IDisposable {
	private readonly ConcurrentQueue<(LogLevel Level, string Source, string Message)> _received = new();
	private readonly LogLevel _previousLevel;

	public PrismLogTests() {
		_previousLevel = PrismLog.SetLevel(LogLevel.Trace);
		PrismLog.SetHandler((level, source, message) => _received.Enqueue((level, source, message)));
	}

	[Fact]
	public void DeliversMessagesToTheHandler() {
		PrismLog.Write(LogLevel.Warning, "Tests", "Hello from the tests");
		PrismLog.Flush();
		Assert.Contains((LogLevel.Warning, "Tests", "Hello from the tests"), Received("Tests"));
	}

	[Fact]
	public void DropsMessagesBelowTheLevel() {
		PrismLog.SetLevel(LogLevel.Error);
		PrismLog.Write(LogLevel.Info, "Tests", "Too quiet");
		PrismLog.Write(LogLevel.Error, "Tests", "Loud enough");
		PrismLog.Flush();
		Assert.Equal(["Loud enough"], Received("Tests").Select(entry => entry.Message));
	}

	[Fact]
	public void SettingTheLevelReturnsTheOneBefore() {
		PrismLog.SetLevel(LogLevel.Debug);
		Assert.Equal(LogLevel.Debug, PrismLog.SetLevel(LogLevel.Info));
	}

	[Fact]
	public void ReplacingTheHandlerMovesDeliveryOver() {
		ConcurrentQueue<string> second = new();
		PrismLog.SetHandler((_, source, message) => {
			if (source == "Tests") second.Enqueue(message);
		});
		PrismLog.Write(LogLevel.Info, "Tests", "To the new one");
		PrismLog.Flush();
		Assert.Equal(["To the new one"], second);
		Assert.Empty(Received("Tests"));
	}

	[Fact]
	public void ANullHandlerDiscardsMessages() {
		PrismLog.SetHandler(null);
		PrismLog.Write(LogLevel.Error, "Tests", "Nobody hears this");
		PrismLog.Flush();
		Assert.Empty(Received("Tests"));
	}

	[Fact]
	public void ACrashingCustomBackendIsLogged() {
		using RegistryBuilder builder = new();
		BackendId id = builder.AddBackend("Crashing", 1, RecordingBackend.Features, () => new RecordingBackend { Failure = new InvalidOperationException("Engine fell over") });
		using Registry registry = builder.Freeze();
		using Prism prism = new(new PrismOptions { Registry = registry });
		using Backend backend = prism.Create(id);
		backend.Initialize();
		Assert.Throws<PrismException>(() => backend.Speak("Hello"));
		PrismLog.Flush();
		Assert.Contains(Received("Refractor"), entry => entry.Level == LogLevel.Error && entry.Message.Contains("Engine fell over"));
	}

	public void Dispose() {
		PrismLog.SetHandler(null);
		PrismLog.SetLevel(_previousLevel);
	}

	private (LogLevel Level, string Source, string Message)[] Received(string source) => [.. _received.Where(entry => entry.Source == source)];
}
