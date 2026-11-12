using Refractor.Interop;

namespace Refractor;

/// <summary>
/// A fixed set of backends from <see cref="RegistryBuilder.Freeze"/>, bound to a context through
/// <see cref="PrismOptions.Registry"/>. Contexts keep their own reference, so this one can be
/// disposed as soon as they are created.
/// </summary>
public sealed unsafe class Registry : IDisposable {
	private readonly RegistryHandle _handle;

	internal Registry(PrismRegistry* registry) => _handle = new RegistryHandle(registry);

	/// <summary>Another reference to the same registry, disposed on its own.</summary>
	public Registry Retain() {
		using Lease lease = Borrow();
		return new(PrismNative.RegistryRetain((PrismRegistry*)lease.Pointer));
	}

	public void Dispose() => _handle.Dispose();

	internal Lease Borrow() => new(_handle, nameof(Registry));
}
