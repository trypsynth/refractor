namespace Refractor;

/// <summary>How severe a log message is, ordered from least to most severe.</summary>
public enum LogLevel {
	Trace,
	Debug,
	Info,
	Warning,
	Error,
	/// <summary>Not a severity. As a threshold it discards every message.</summary>
	None
}
