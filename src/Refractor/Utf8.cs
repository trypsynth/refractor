using System.Runtime.InteropServices;
using System.Text;

namespace Refractor;

internal static unsafe class Utf8 {
	// prism reads up to the first null, so a string that carries one would be cut short without
	// any error. Refusing it keeps what is spoken the same as what was passed.
	internal static byte[] Encode(string value, string parameterName) {
		ArgumentNullException.ThrowIfNull(value, parameterName);
		if (value.Contains('\0')) throw new ArgumentException("The string contains a null character.", parameterName);
		byte[] bytes = new byte[Encoding.UTF8.GetByteCount(value) + 1];
		Encoding.UTF8.GetBytes(value, bytes);
		return bytes;
	}

	internal static string? Decode(byte* value) => value is null ? null : Marshal.PtrToStringUTF8((nint)value);
}
