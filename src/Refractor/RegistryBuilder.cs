using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>Collects custom and plugin backends into a <see cref="Registry"/>. It starts with every built-in backend.</summary>
public sealed unsafe class RegistryBuilder : IDisposable {
	private readonly RegistryBuilderHandle _handle;

	public RegistryBuilder() {
		PrismRegistryBuilder* builder = PrismNative.RegistryBuilderNew();
		if (builder is null) throw new OutOfMemoryException("prism could not create a registry builder.");
		_handle = new RegistryBuilderHandle(builder);
	}

	/// <summary>
	/// Registers a backend written in .NET. <paramref name="factory"/> makes one instance per
	/// backend prism creates, and returning null fails that creation. <paramref name="features"/>
	/// must list exactly the operations the backend overrides: prism only calls those.
	/// </summary>
	/// <returns>The id prism derived from <paramref name="name"/>.</returns>
	public BackendId AddBackend(string name, int priority, BackendFeatures features, Func<CustomBackend?> factory) {
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentOutOfRangeException.ThrowIfNegative(priority);
		byte[] nativeName = Utf8.Encode(name, nameof(name));
		PrismBackendVTable vtable = CustomBackendBridge.CreateVTable(features);
		using Lease lease = Borrow();
		GCHandle factoryHandle = GCHandle.Alloc(factory);
		ulong id;
		PrismError error;
		// prism owns the factory handle from here and frees it through the callback exactly
		// once, even when registration fails, so it is never freed on this side.
		fixed (byte* text = nativeName) {
			error = PrismNative.RegistryBuilderAddBackend(Native(lease), text, priority, (ulong)features, &vtable, (void*)GCHandle.ToIntPtr(factoryHandle), CustomBackendBridge.FreeFactory, &id);
		}
		PrismException.ThrowIfFailed(error);
		return new BackendId(id);
	}

	/// <summary>
	/// Loads a prism plugin library and registers every backend in it. This runs the library's
	/// own code. <paramref name="priorityOverride"/> replaces each backend's declared priority.
	/// </summary>
	/// <returns>How many backends were added.</returns>
	public int AddLibrary(string path, int? priorityOverride = null) {
		if (priorityOverride is int priority) ArgumentOutOfRangeException.ThrowIfNegative(priority, nameof(priorityOverride));
		using Lease lease = Borrow();
		nuint count;
		fixed (byte* text = Utf8.Encode(path, nameof(path))) {
			PrismException.ThrowIfFailed(PrismNative.RegistryBuilderAddLibrary(Native(lease), text, priorityOverride ?? -1, &count));
		}
		return checked((int)count);
	}

	/// <summary>Moves every registration into a new registry. The builder is spent afterwards but still needs disposing.</summary>
	public Registry Freeze() {
		using Lease lease = Borrow();
		PrismRegistry* registry = PrismNative.RegistryFreeze(Native(lease));
		return registry is not null ? new Registry(registry) : throw new InvalidOperationException("The builder was already frozen, or prism ran out of memory.");
	}

	public void Dispose() => _handle.Dispose();

	private Lease Borrow() => new(_handle, nameof(RegistryBuilder));

	private static PrismRegistryBuilder* Native(in Lease lease) => (PrismRegistryBuilder*)lease.Pointer;
}
