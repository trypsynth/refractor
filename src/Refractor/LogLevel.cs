namespace Refractor;

/// <summary>How severe a log message is, ordered from least to most severe.</summary>
public enum LogLevel {
	/// <summary>Step by step detail, for following what prism does.</summary>
	Trace,
	/// <summary>Detail useful when debugging.</summary>
	Debug,
	/// <summary>Normal events worth knowing about.</summary>
	Info,
	/// <summary>Something went wrong, but prism carried on.</summary>
	Warning,
	/// <summary>An operation failed.</summary>
	Error,
	/// <summary>Not a severity. As a threshold it discards every message.</summary>
	None
}
