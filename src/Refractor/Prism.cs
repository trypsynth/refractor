using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>
/// A prism context: the entry point for finding and creating backends. Registry calls are
/// thread safe. Backends it hands out stay usable after it is disposed.
/// </summary>
public sealed unsafe class Prism : IDisposable {
	private PrismContext* _context;
	private GCHandle _callbacks;

	public Prism(PrismOptions? options = null) {
		PrismConfig config = PrismNative.ConfigInit();
		if (options is not null) Configure(ref config, options);
		_context = PrismNative.Init(&config);
		GC.KeepAlive(options?.Registry);
		if (_context is not null) return;
		if (_callbacks.IsAllocated) _callbacks.Free();
		throw new PrismException(PrismError.Internal, "prism could not create a context.");
	}

	~Prism() => Release();

	public static Version Version {
		get {
			uint packed = PrismNative.Version();
			return new Version((int)((packed >> 16) & 0xFF), (int)((packed >> 8) & 0xFF), (int)(packed & 0xFF));
		}
	}

	/// <summary>The full version prism was built as, which can carry more than <see cref="Version"/>.</summary>
	public static string VersionString => Utf8.Decode(PrismNative.VersionString()) ?? string.Empty;

	public static bool IsAutoPowerManagementSupported => PrismNative.AvailabilityAutoPowerSupported();

	public static string GetErrorMessage(PrismError error) => Utf8.Decode(PrismNative.ErrorString(error)) ?? error.ToString();

	/// <summary>Registered backends, which is not the same as usable ones.</summary>
	public int BackendCount => checked((int)PrismNative.RegistryCount(Context));

	/// <summary>Every registered backend, highest priority first.</summary>
	public IReadOnlyList<BackendId> BackendIds {
		get {
			PrismContext* context = Context;
			BackendId[] ids = new BackendId[checked((int)PrismNative.RegistryCount(context))];
			for (int index = 0; index < ids.Length; index++) ids[index] = new BackendId(PrismNative.RegistryIdAt(context, (nuint)index));
			return ids;
		}
	}

	/// <summary>Finds a backend by its exact, case sensitive name, such as "NVDA".</summary>
	public BackendId? FindBackend(string name) {
		fixed (byte* text = Utf8.Encode(name, nameof(name))) {
			ulong id = PrismNative.RegistryId(Context, text);
			return id == 0 ? null : new BackendId(id);
		}
	}

	public string? GetBackendName(BackendId id) => Utf8.Decode(PrismNative.RegistryName(Context, id.Value));

	/// <summary>Higher runs first in <see cref="CreateBest"/>. Null when the id is not registered.</summary>
	public int? GetBackendPriority(BackendId id) {
		int priority = PrismNative.RegistryPriority(Context, id.Value);
		return priority < 0 ? null : priority;
	}

	/// <summary>Whether the backend is registered. It says nothing about whether it will start.</summary>
	public bool HasBackend(BackendId id) => PrismNative.RegistryExists(Context, id.Value);

	/// <summary>Another handle to a shared instance from <see cref="Acquire"/>, or null when none is alive.</summary>
	public Backend? GetCachedBackend(BackendId id) {
		PrismBackend* backend = PrismNative.RegistryGet(Context, id.Value);
		return backend is null ? null : new Backend(backend);
	}

	/// <summary>A new instance with its own state. Call <see cref="Backend.Initialize"/> before using it.</summary>
	public Backend Create(BackendId id) => Backend.Wrap(PrismNative.RegistryCreate(Context, id.Value));

	/// <summary>
	/// A new, initialized instance of the highest priority backend that starts. A running screen
	/// reader wins over standalone speech. This is the usual way in.
	/// </summary>
	public Backend CreateBest() => Backend.Wrap(PrismNative.RegistryCreateBest(Context));

	/// <summary>
	/// A shared instance, reused while any handle to it is alive, so voice and rate changes are
	/// seen by every holder. Call <see cref="Backend.Initialize"/>; it may already be initialized.
	/// </summary>
	public Backend Acquire(BackendId id) => Backend.Wrap(PrismNative.RegistryAcquire(Context, id.Value));

	/// <summary>The shared counterpart of <see cref="CreateBest"/>. Always initialized.</summary>
	public Backend AcquireBest() => Backend.Wrap(PrismNative.RegistryAcquireBest(Context));

	public void PauseAvailabilityPolling() => PrismNative.AvailabilityPollPause(Context);

	/// <summary>Resumes polling with an immediate scan that reports every net change since the pause.</summary>
	public void ResumeAvailabilityPolling() => PrismNative.AvailabilityPollResume(Context);

	public void Dispose() {
		Release();
		GC.SuppressFinalize(this);
	}

	private PrismContext* Context => _context is not null ? _context : throw new ObjectDisposedException(nameof(Prism));

	private void Configure(ref PrismConfig config, PrismOptions options) {
		if (options.Registry is Registry registry) config.Registry = registry.Pointer;
		config.AvailabilityPollIntervalMs = Milliseconds(options.PollInterval);
		config.AvailabilityBackoffMaxMs = Milliseconds(options.MaxBackoff);
		config.AvailabilityDebounceSamples = options.DebounceSamples is int samples ? (uint)Math.Max(samples, 1) : 0;
		config.AvailabilityAutoPowerManage = options.AutoPowerManagement ? (byte)1 : (byte)0;
		if (options.AvailabilityChanged is null) return;
		_callbacks = GCHandle.Alloc(new AvailabilityCallbacks(options.AvailabilityChanged, options.AvailabilityBaselineEstablished));
		config.AvailabilityUserdata = (void*)GCHandle.ToIntPtr(_callbacks);
		config.AvailabilityCallback = &OnAvailabilityChanged;
		if (options.AvailabilityBaselineEstablished is not null) config.AvailabilityBaselineCallback = &OnAvailabilityBaseline;
	}

	// Shutdown joins the poll thread, so the callbacks can only be freed once it has returned.
	private void Release() {
		if (_context is not null) {
			PrismNative.Shutdown(_context);
			_context = null;
		}
		if (_callbacks.IsAllocated) _callbacks.Free();
	}

	private static uint Milliseconds(TimeSpan? span) => span is TimeSpan value ? (uint)Math.Clamp(value.TotalMilliseconds, 1, uint.MaxValue) : 0;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAvailabilityChanged(void* userdata, ulong backend, byte* name, byte available) {
		AvailabilityCallbacks callbacks = (AvailabilityCallbacks)GCHandle.FromIntPtr((nint)userdata).Target!;
		callbacks.Changed(new BackendId(backend), Utf8.Decode(name) ?? string.Empty, available != 0);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAvailabilityBaseline(void* userdata) {
		AvailabilityCallbacks callbacks = (AvailabilityCallbacks)GCHandle.FromIntPtr((nint)userdata).Target!;
		callbacks.Baseline?.Invoke();
	}

	private sealed record AvailabilityCallbacks(AvailabilityChangedHandler Changed, Action? Baseline);
}
