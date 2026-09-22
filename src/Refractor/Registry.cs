using Refractor.Interop;

namespace Refractor;

/// <summary>
/// A fixed set of backends from <see cref="RegistryBuilder.Freeze"/>, bound to a context through
/// <see cref="PrismOptions.Registry"/>. Contexts keep their own reference, so this one can be
/// disposed as soon as they are created.
/// </summary>
public sealed unsafe class Registry : IDisposable {
	private PrismRegistry* _registry;

	internal Registry(PrismRegistry* registry) => _registry = registry;

	~Registry() => Release();

	/// <summary>Another reference to the same registry, disposed on its own.</summary>
	public Registry Retain() => new(PrismNative.RegistryRetain(Pointer));

	public void Dispose() {
		Release();
		GC.SuppressFinalize(this);
	}

	internal PrismRegistry* Pointer => _registry is not null ? _registry : throw new ObjectDisposedException(nameof(Registry));

	private void Release() {
		if (_registry is null) return;
		PrismNative.RegistryRelease(_registry);
		_registry = null;
	}
}
