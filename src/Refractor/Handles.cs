using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

// Each prism object Refractor owns sits in a SafeHandle. A wrapper holds a Lease on its handle
// across every native call, so the object cannot be freed while prism is using it: not by Dispose
// on another thread, and not by the finalizer, which could otherwise run as soon as the call has
// read the pointer and the wrapper is no longer referenced.
internal abstract class PrismHandle : SafeHandle {
	protected PrismHandle(nint handle) : base(0, ownsHandle: true) => SetHandle(handle);

	public override bool IsInvalid => handle == 0;
}

internal sealed unsafe class BackendHandle(PrismBackend* backend) : PrismHandle((nint)backend) {
	protected override bool ReleaseHandle() {
		PrismNative.BackendFree((PrismBackend*)handle);
		return true;
	}
}

internal sealed unsafe class RegistryHandle(PrismRegistry* registry) : PrismHandle((nint)registry) {
	protected override bool ReleaseHandle() {
		PrismNative.RegistryRelease((PrismRegistry*)handle);
		return true;
	}
}

internal sealed unsafe class RegistryBuilderHandle(PrismRegistryBuilder* builder) : PrismHandle((nint)builder) {
	protected override bool ReleaseHandle() {
		PrismNative.RegistryBuilderFree((PrismRegistryBuilder*)handle);
		return true;
	}
}

// Shutting a context down joins its poll thread, so the availability callbacks it calls are only
// freed once that has returned.
internal sealed unsafe class ContextHandle(PrismContext* context, GCHandle callbacks) : PrismHandle((nint)context) {
	protected override bool ReleaseHandle() {
		PrismNative.Shutdown((PrismContext*)handle);
		if (callbacks.IsAllocated) callbacks.Free();
		return true;
	}
}

internal readonly unsafe ref struct Lease {
	private readonly SafeHandle _handle;

	// The owner names the public type in the exception, rather than the handle's.
	public Lease(SafeHandle handle, string owner) {
		if (handle.IsClosed) throw new ObjectDisposedException(owner);
		bool added = false;
		try {
			handle.DangerousAddRef(ref added);
		} catch (ObjectDisposedException) {
			throw new ObjectDisposedException(owner);
		}
		_handle = handle;
		Pointer = (void*)handle.DangerousGetHandle();
	}

	public void* Pointer { get; }

	public void Dispose() => _handle.DangerousRelease();
}
