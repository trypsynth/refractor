namespace Refractor;

/// <summary>Thrown when prism reports an error. <see cref="Error"/> holds the code.</summary>
public sealed class PrismException : Exception {
	/// <summary>An exception for <paramref name="error"/>, with prism's description of it as the message.</summary>
	public PrismException(PrismError error) : base(Prism.GetErrorMessage(error)) => Error = error;

	/// <summary>An exception for <paramref name="error"/>, with a message of its own.</summary>
	public PrismException(PrismError error, string message) : base(message) => Error = error;

	/// <summary>The error prism reported.</summary>
	public PrismError Error { get; }

	internal static void ThrowIfFailed(PrismError error) {
		if (error != PrismError.Ok) throw new PrismException(error);
	}
}
