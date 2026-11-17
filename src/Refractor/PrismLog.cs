using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

/// <summary>One of prism's log messages.</summary>
public sealed class LogMessageEventArgs(LogLevel level, string source, string message) : EventArgs {
	public LogLevel Level { get; } = level;

	/// <summary>What wrote it, such as a backend's name, or "Refractor".</summary>
	public string Source { get; } = source;

	public string Message { get; } = message;
}

/// <summary>
/// prism's process wide logger. Usable before any <see cref="Prism"/> exists. Setting the
/// PRISM_LOG environment variable to a level name makes prism log to standard error on its own,
/// until a handler is added to <see cref="MessageLogged"/>.
/// </summary>
public static unsafe class PrismLog {
	private static readonly object Gate = new();
	private static EventHandler<LogMessageEventArgs>? _messageLogged;

	/// <summary>
	/// Raised for each message at or above the level, with a null sender. Handlers run on prism's
	/// logging thread, one message at a time, and should be quick. They must not add or remove
	/// handlers, or flush, from there. An exception one throws is dropped, and the others still run.
	/// </summary>
	public static event EventHandler<LogMessageEventArgs>? MessageLogged {
		add {
			lock (Gate) {
				bool first = _messageLogged is null;
				_messageLogged += value;
				if (first && _messageLogged is not null) PrismNative.SetLogHandler(new PrismLogHandler { Callback = &Deliver });
			}
		}
		remove {
			lock (Gate) {
				if (_messageLogged is null) return;
				_messageLogged -= value;
				// Without a handler of its own, prism drops messages instead of queueing them.
				if (_messageLogged is null) PrismNative.SetLogHandler(default);
			}
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

	// For an exception Refractor caught where nobody could be told, from a custom backend or an
	// availability handler. Logging must not fail in turn, so nothing escapes this either.
	internal static void WriteFailure(string what, Exception error) {
		try {
			Write(LogLevel.Error, "Refractor", $"{what} threw {error.GetType().FullName}: {error.Message}".Replace('\0', ' '));
		} catch (Exception) {
		}
	}

	/// <summary>Waits until everything queued before the call has been delivered.</summary>
	public static void Flush() => PrismNative.LogFlush();

	/// <summary>Flushes and stops the logger.</summary>
	public static void Shutdown() => PrismNative.LogShutdown();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Deliver(void* userdata, LogLevel level, byte* source, byte* message) {
		// A failing log handler has nowhere to report to, and taking the process down over a log
		// line would be worse than losing it, so nothing here may throw back into prism.
		try {
			EventHandler<LogMessageEventArgs>? handlers = Volatile.Read(ref _messageLogged);
			if (handlers is null) return;
			LogMessageEventArgs args = new(level, Utf8.Decode(source) ?? string.Empty, Utf8.Decode(message) ?? string.Empty);
			// Each handler is called on its own, so one that throws cannot keep the message from the rest.
			foreach (Delegate handler in handlers.GetInvocationList()) {
				try {
					((EventHandler<LogMessageEventArgs>)handler)(null, args);
				} catch (Exception) {
				}
			}
		} catch (Exception) {
		}
	}
}
