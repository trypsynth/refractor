using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>
/// A prism context: the entry point for finding and creating backends. Registry calls are
/// thread safe. Backends it hands out stay usable after it is disposed.
/// </summary>
public sealed unsafe class Prism : IDisposable {
	private readonly ContextHandle _handle;

	public Prism(PrismOptions? options = null) {
		PrismConfig config = PrismNative.ConfigInit();
		GCHandle callbacks = default;
		PrismContext* context;
		Registry? source = options?.Registry;
		Lease registry = source is not null ? source.Borrow() : default;
		try {
			if (options is not null) callbacks = Configure(ref config, options, registry);
			context = PrismNative.Init(&config);
		} finally {
			// The context holds its own reference to the registry once it exists.
			if (source is not null) registry.Dispose();
		}
		if (context is null) {
			if (callbacks.IsAllocated) callbacks.Free();
			throw new PrismException(PrismError.Internal, "prism could not create a context.");
		}
		_handle = new ContextHandle(context, callbacks);
	}

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

	/// <summary>Every registered backend, highest priority first. Registered is not the same as usable.</summary>
	public IReadOnlyList<BackendId> GetBackendIds() {
		using Lease lease = Borrow();
		PrismContext* context = Native(lease);
		BackendId[] ids = new BackendId[checked((int)PrismNative.RegistryCount(context))];
		for (int index = 0; index < ids.Length; index++) ids[index] = new BackendId(PrismNative.RegistryIdAt(context, (nuint)index));
		return ids;
	}

	/// <summary>Finds a backend by its exact, case sensitive name, such as "NVDA".</summary>
	public BackendId? FindBackend(string name) {
		using Lease lease = Borrow();
		fixed (byte* text = Utf8.Encode(name, nameof(name))) {
			ulong id = PrismNative.RegistryId(Native(lease), text);
			return id == 0 ? null : new BackendId(id);
		}
	}

	public string? GetBackendName(BackendId id) {
		using Lease lease = Borrow();
		return Utf8.Decode(PrismNative.RegistryName(Native(lease), id.Value));
	}

	/// <summary>Higher runs first in <see cref="CreateBest"/>. Null when the id is not registered.</summary>
	public int? GetBackendPriority(BackendId id) {
		using Lease lease = Borrow();
		int priority = PrismNative.RegistryPriority(Native(lease), id.Value);
		return priority < 0 ? null : priority;
	}

	/// <summary>Whether the backend is registered. It says nothing about whether it will start.</summary>
	public bool HasBackend(BackendId id) {
		using Lease lease = Borrow();
		return PrismNative.RegistryExists(Native(lease), id.Value);
	}

	/// <summary>Another handle to a shared instance from <see cref="Acquire"/>, or null when none is alive.</summary>
	public Backend? GetCachedBackend(BackendId id) {
		using Lease lease = Borrow();
		PrismBackend* backend = PrismNative.RegistryGet(Native(lease), id.Value);
		return backend is null ? null : new Backend(backend);
	}

	/// <summary>A new instance with its own state. Call <see cref="Backend.Initialize"/> before using it.</summary>
	public Backend Create(BackendId id) {
		using Lease lease = Borrow();
		return Backend.Wrap(PrismNative.RegistryCreate(Native(lease), id.Value));
	}

	/// <summary>
	/// A new, initialized instance of the highest priority backend that starts. A running screen
	/// reader wins over standalone speech. This is the usual way in.
	/// </summary>
	public Backend CreateBest() {
		using Lease lease = Borrow();
		return Backend.Wrap(PrismNative.RegistryCreateBest(Native(lease)));
	}

	/// <summary>
	/// A shared instance, reused while any handle to it is alive, so voice and rate changes are
	/// seen by every holder. Call <see cref="Backend.Initialize"/>; it may already be initialized.
	/// </summary>
	public Backend Acquire(BackendId id) {
		using Lease lease = Borrow();
		return Backend.Wrap(PrismNative.RegistryAcquire(Native(lease), id.Value));
	}

	/// <summary>The shared counterpart of <see cref="CreateBest"/>. Always initialized.</summary>
	public Backend AcquireBest() {
		using Lease lease = Borrow();
		return Backend.Wrap(PrismNative.RegistryAcquireBest(Native(lease)));
	}

	public void PauseAvailabilityPolling() {
		using Lease lease = Borrow();
		PrismNative.AvailabilityPollPause(Native(lease));
	}

	/// <summary>Resumes polling with an immediate scan that reports every net change since the pause.</summary>
	public void ResumeAvailabilityPolling() {
		using Lease lease = Borrow();
		PrismNative.AvailabilityPollResume(Native(lease));
	}

	public void Dispose() => _handle.Dispose();

	private Lease Borrow() => new(_handle, nameof(Prism));

	private static PrismContext* Native(in Lease lease) => (PrismContext*)lease.Pointer;

	private static GCHandle Configure(ref PrismConfig config, PrismOptions options, in Lease registry) {
		if (options.Registry is not null) config.Registry = (PrismRegistry*)registry.Pointer;
		config.AvailabilityPollIntervalMs = Milliseconds(options.PollInterval);
		config.AvailabilityBackoffMaxMs = Milliseconds(options.MaxBackoff);
		config.AvailabilityDebounceSamples = options.DebounceSamples is int samples ? (uint)samples : 0;
		config.AvailabilityAutoPowerManage = options.AutoPowerManagement ? (byte)1 : (byte)0;
		if (options.AvailabilityChanged is null) return default;
		GCHandle callbacks = GCHandle.Alloc(new AvailabilityCallbacks(options.AvailabilityChanged, options.AvailabilityBaselineEstablished));
		config.AvailabilityUserdata = (void*)GCHandle.ToIntPtr(callbacks);
		config.AvailabilityCallback = &OnAvailabilityChanged;
		if (options.AvailabilityBaselineEstablished is not null) config.AvailabilityBaselineCallback = &OnAvailabilityBaseline;
		return callbacks;
	}

	// PrismOptions has already checked the range, and 0 means prism's default.
	private static uint Milliseconds(TimeSpan? span) => span is TimeSpan value ? (uint)value.TotalMilliseconds : 0;

	// An exception cannot cross back into prism: one leaving an UnmanagedCallersOnly method ends the
	// process. A handler's exception has nobody to report to on the poll thread, so it goes to
	// prism's log and polling carries on.
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAvailabilityChanged(void* userdata, ulong backend, byte* name, byte available) {
		AvailabilityCallbacks callbacks = (AvailabilityCallbacks)GCHandle.FromIntPtr((nint)userdata).Target!;
		try {
			callbacks.Changed(new BackendId(backend), Utf8.Decode(name) ?? string.Empty, available != 0);
		} catch (Exception error) {
			PrismLog.WriteFailure("An availability handler", error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAvailabilityBaseline(void* userdata) {
		AvailabilityCallbacks callbacks = (AvailabilityCallbacks)GCHandle.FromIntPtr((nint)userdata).Target!;
		try {
			callbacks.Baseline?.Invoke();
		} catch (Exception error) {
			PrismLog.WriteFailure("An availability baseline handler", error);
		}
	}

	private sealed record AvailabilityCallbacks(AvailabilityChangedHandler Changed, Action? Baseline);
}
