using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>Receives one of prism's log messages.</summary>
public delegate void LogHandler(LogLevel level, string source, string message);

/// <summary>
/// prism's process wide logger. Usable before any <see cref="Prism"/> exists. Setting the
/// PRISM_LOG environment variable to a level name makes prism log to standard error on its own.
/// </summary>
public static unsafe class PrismLog {
	private static readonly object Gate = new();
	private static GCHandle _handler;

	/// <summary>
	/// Replaces the handler. It runs on prism's logging thread, never twice at once, and must be
	/// quick and must not call back into <see cref="PrismLog"/>. Null discards messages.
	/// </summary>
	public static void SetHandler(LogHandler? handler) {
		lock (Gate) {
			GCHandle previous = _handler;
			_handler = handler is null ? default : GCHandle.Alloc(handler);
			PrismLogHandler native = handler is null ? default : new PrismLogHandler { Callback = &Deliver, Userdata = (void*)GCHandle.ToIntPtr(_handler) };
			PrismNative.SetLogHandler(native);
			if (!previous.IsAllocated) return;
			// A message already on its way can still reach the old handler, so it stays alive
			// until everything queued so far has been delivered.
			PrismNative.LogFlush();
			previous.Free();
		}
	}

	/// <summary>Sets the lowest level delivered and returns the one before.</summary>
	public static LogLevel SetLevel(LogLevel level) => PrismNative.SetLogLevel(level);

	/// <summary>Queues a message through prism's logger, below the level it is dropped.</summary>
	public static void Write(LogLevel level, string source, string message) {
		fixed (byte* sourceText = Utf8.Encode(source, nameof(source)))
		fixed (byte* messageText = Utf8.Encode(message, nameof(message))) {
			PrismNative.Log(level, sourceText, messageText);
		}
	}

	/// <summary>Waits until everything queued before the call has been delivered.</summary>
	public static void Flush() => PrismNative.LogFlush();

	/// <summary>Flushes and stops the logger.</summary>
	public static void Shutdown() => PrismNative.LogShutdown();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Deliver(void* userdata, LogLevel level, byte* source, byte* message) {
		LogHandler handler = (LogHandler)GCHandle.FromIntPtr((nint)userdata).Target!;
		try {
			handler(level, Utf8.Decode(source) ?? string.Empty, Utf8.Decode(message) ?? string.Empty);
		} catch (Exception) {
			// A failing log handler has nowhere to report to, and taking the process down over a
			// log line would be worse than losing it.
		}
	}
}
