namespace Refractor;

/// <summary>Thrown when prism reports an error. <see cref="Error"/> holds the code.</summary>
public sealed class PrismException : Exception {
	public PrismException(PrismError error) : base(Prism.GetErrorMessage(error)) => Error = error;

	public PrismException(PrismError error, string message) : base(message) => Error = error;

	public PrismError Error { get; }

	internal static void ThrowIfFailed(PrismError error) {
		if (error != PrismError.Ok) throw new PrismException(error);
	}
}
