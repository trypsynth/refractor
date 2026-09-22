namespace Refractor;

/// <summary>Reports that a backend became available (true) or unavailable (false).</summary>
public delegate void AvailabilityChangedHandler(BackendId backend, string name, bool isAvailable);

/// <summary>Settings for a new <see cref="Prism"/> context.</summary>
public sealed class PrismOptions {
	/// <summary>The registry to bind to, for custom and plugin backends. Null means prism's built-in set.</summary>
	public Registry? Registry { get; init; }

	/// <summary>
	/// Turns on background availability polling. Runs on prism's poll thread, never the caller's.
	/// A backend that is already available when polling starts is not reported.
	/// </summary>
	public AvailabilityChangedHandler? AvailabilityChanged { get; init; }

	/// <summary>
	/// Runs once on the poll thread after the first scan, before any change is reported. Only
	/// needed when a decision made from an earlier direct check has to be checked again.
	/// Ignored unless <see cref="AvailabilityChanged"/> is set.
	/// </summary>
	public Action? AvailabilityBaselineEstablished { get; init; }

	/// <summary>Time between scans. Null means prism's default of one second.</summary>
	public TimeSpan? PollInterval { get; init; }

	/// <summary>Scans that must agree before a change is reported. Null means prism's default of two.</summary>
	public int? DebounceSamples { get; init; }

	/// <summary>Longest interval the scans back off to while nothing changes. Null turns backoff off.</summary>
	public TimeSpan? MaxBackoff { get; init; }

	/// <summary>Pauses polling while the system sleeps, where <see cref="Prism.IsAutoPowerManagementSupported"/>.</summary>
	public bool AutoPowerManagement { get; init; }
}
