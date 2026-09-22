namespace Refractor.Tests;

public class PrismTests {
	[Fact]
	public void ReportsTheVersionItWasBuiltAs() {
		Assert.True(Prism.Version >= new Version(0, 18, 0), Prism.Version.ToString());
		Assert.StartsWith($"{Prism.Version.Major}.{Prism.Version.Minor}.{Prism.Version.Build}", Prism.VersionString);
	}

	[Fact]
	public void DescribesEveryError() {
		Assert.All(Enum.GetValues<PrismError>(), error => Assert.False(string.IsNullOrWhiteSpace(Prism.GetErrorMessage(error))));
		Assert.NotEqual(Prism.GetErrorMessage(PrismError.Ok), Prism.GetErrorMessage(PrismError.Internal));
	}

	[Fact]
	public void AnExceptionCarriesPrismsMessage() {
		PrismException error = new(PrismError.VoiceNotFound);
		Assert.Equal(PrismError.VoiceNotFound, error.Error);
		Assert.Equal(Prism.GetErrorMessage(PrismError.VoiceNotFound), error.Message);
	}

	[Fact]
	public void ListsTheWindowsBackends() {
		using Prism prism = new();
		Assert.Equal(prism.BackendCount, prism.BackendIds.Count);
		Assert.Contains(BackendId.Sapi, prism.BackendIds);
		Assert.Contains(BackendId.Nvda, prism.BackendIds);
		Assert.Contains(BackendId.OneCore, prism.BackendIds);
	}

	[Fact]
	public void ListsBackendsHighestPriorityFirst() {
		using Prism prism = new();
		int[] priorities = [.. prism.BackendIds.Select(id => prism.GetBackendPriority(id) ?? -1)];
		Assert.Equal(priorities.OrderDescending(), priorities);
	}

	[Fact]
	public void TheKnownIdsMatchTheirNames() {
		using Prism prism = new();
		Assert.Equal("SAPI", prism.GetBackendName(BackendId.Sapi));
		Assert.Equal("NVDA", prism.GetBackendName(BackendId.Nvda));
		Assert.Equal("JAWS", prism.GetBackendName(BackendId.Jaws));
		Assert.Equal("OneCore", prism.GetBackendName(BackendId.OneCore));
		Assert.Equal(BackendId.ZoomText, prism.FindBackend("ZoomText"));
	}

	[Fact]
	public void FindsBackendsByExactName() {
		using Prism prism = new();
		Assert.Equal(BackendId.Nvda, prism.FindBackend("NVDA"));
		Assert.Null(prism.FindBackend("nvda"));
		Assert.Null(prism.FindBackend("Nothing by this name"));
	}

	[Fact]
	public void AnUnknownIdHasNoNameOrPriority() {
		using Prism prism = new();
		Assert.False(prism.HasBackend(BackendId.Invalid));
		Assert.Null(prism.GetBackendName(new BackendId(42)));
		Assert.Null(prism.GetBackendPriority(new BackendId(42)));
		Assert.True(prism.HasBackend(BackendId.Sapi));
	}

	[Fact]
	public void CreatingAnUnknownBackendThrows() {
		using Prism prism = new();
		PrismException error = Assert.Throws<PrismException>(() => prism.Create(new BackendId(42)));
		Assert.Equal(PrismError.BackendNotAvailable, error.Error);
	}

	[Fact]
	public void NothingIsCachedUntilAcquired() {
		using Prism prism = new();
		Assert.Null(prism.GetCachedBackend(BackendId.Sapi));
	}

	[Fact]
	public void AStringWithANullCharacterIsRefused() {
		using Prism prism = new();
		Assert.Throws<ArgumentException>(() => prism.FindBackend("NV\0DA"));
	}

	[Fact]
	public void ADisposedContextRefusesToWork() {
		Prism prism = new();
		prism.Dispose();
		Assert.Throws<ObjectDisposedException>(() => prism.BackendCount);
		prism.Dispose();
	}

	[Fact]
	public void BackendIdsPrintAsHex() {
		Assert.Equal("0x89CC19C5C4AC1A56", BackendId.Nvda.ToString());
		Assert.False(BackendId.Invalid.IsValid);
		Assert.True(BackendId.Sapi.IsValid);
	}
}
