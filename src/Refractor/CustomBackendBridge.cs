using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Refractor.Interop;

namespace Refractor;

// prism calls a custom backend through these. Every entry point catches everything, because an
// exception cannot unwind back through prism, and turns it into the error prism returns. The
// operations go through Run and Get, whose lambdas are static so no call allocates for them.
internal static unsafe class CustomBackendBridge {
	internal static delegate* unmanaged[Cdecl]<void*, void> FreeFactory => &FreeFactoryCore;

	internal static PrismBackendVTable CreateVTable(BackendFeatures features) {
		PrismBackendVTable vtable = new() {
			Size = (nuint)sizeof(PrismBackendVTable),
			Create = &Create,
			Destroy = &Destroy,
			IsSupported = &IsSupported,
			Initialize = &Initialize
		};
		// prism rejects a registration whose operations and declared features disagree, so an
		// operation is installed exactly when its feature is declared.
		if (Has(features, BackendFeatures.Speak)) vtable.Speak = &Speak;
		if (Has(features, BackendFeatures.SpeakToMemory)) vtable.SpeakToMemory = &SpeakToMemory;
		if (Has(features, BackendFeatures.Braille)) vtable.Braille = &Braille;
		if (Has(features, BackendFeatures.Output)) vtable.Output = &Output;
		if (Has(features, BackendFeatures.Stop)) vtable.Stop = &Stop;
		if (Has(features, BackendFeatures.Pause)) vtable.Pause = &Pause;
		if (Has(features, BackendFeatures.Resume)) vtable.Resume = &Resume;
		if (Has(features, BackendFeatures.IsSpeaking)) vtable.IsSpeaking = &IsSpeaking;
		if (Has(features, BackendFeatures.SetVolume)) vtable.SetVolume = &SetVolume;
		if (Has(features, BackendFeatures.GetVolume)) vtable.GetVolume = &GetVolume;
		if (Has(features, BackendFeatures.SetRate)) vtable.SetRate = &SetRate;
		if (Has(features, BackendFeatures.GetRate)) vtable.GetRate = &GetRate;
		if (Has(features, BackendFeatures.SetPitch)) vtable.SetPitch = &SetPitch;
		if (Has(features, BackendFeatures.GetPitch)) vtable.GetPitch = &GetPitch;
		if (Has(features, BackendFeatures.RefreshVoices)) vtable.RefreshVoices = &RefreshVoices;
		if (Has(features, BackendFeatures.CountVoices)) vtable.CountVoices = &CountVoices;
		if (Has(features, BackendFeatures.GetVoiceName)) vtable.GetVoiceName = &GetVoiceName;
		if (Has(features, BackendFeatures.GetVoiceLanguage)) vtable.GetVoiceLanguage = &GetVoiceLanguage;
		if (Has(features, BackendFeatures.SetVoice)) vtable.SetVoice = &SetVoice;
		if (Has(features, BackendFeatures.GetVoice)) vtable.GetVoice = &GetVoice;
		if (Has(features, BackendFeatures.GetChannels)) vtable.GetChannels = &GetChannels;
		if (Has(features, BackendFeatures.GetSampleRate)) vtable.GetSampleRate = &GetSampleRate;
		if (Has(features, BackendFeatures.GetBitDepth)) vtable.GetBitDepth = &GetBitDepth;
		return vtable;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void FreeFactoryCore(void* userdata) => GCHandle.FromIntPtr((nint)userdata).Free();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void* Create(void* userdata) {
		try {
			Func<CustomBackend?> factory = (Func<CustomBackend?>)GCHandle.FromIntPtr((nint)userdata).Target!;
			CustomBackend? backend = factory();
			return backend is null ? null : (void*)GCHandle.ToIntPtr(GCHandle.Alloc(new Instance(backend)));
		} catch (Exception error) {
			PrismLog.WriteFailure("A custom backend factory", error);
			return null;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Destroy(void* instance) {
		GCHandle handle = GCHandle.FromIntPtr((nint)instance);
		Instance state = (Instance)handle.Target!;
		try {
			(state.Backend as IDisposable)?.Dispose();
		} catch (Exception error) {
			PrismLog.WriteFailure("A custom backend's Dispose", error);
		} finally {
			state.ReleaseText();
			handle.Free();
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte IsSupported(void* instance) {
		try {
			return Backend(instance).IsSupported() ? (byte)1 : (byte)0;
		} catch (Exception error) {
			PrismLog.WriteFailure("A custom backend's IsSupported", error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Initialize(void* instance) => Run(instance, static backend => backend.Initialize());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Speak(void* instance, byte* text, byte interrupt) => Run(instance, ((nint)text, interrupt != 0), static (backend, call) => backend.Speak(Text(call.Item1), call.Item2));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SpeakToMemory(void* instance, byte* text, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata) {
		// AudioSink is a ref struct, so it cannot pass through a lambda.
		try {
			Backend(instance).SpeakToMemory(Text((nint)text), new AudioSink(callback, userdata));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Braille(void* instance, byte* text) => Run(instance, (nint)text, static (backend, text) => backend.Braille(Text(text)));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Output(void* instance, byte* text, byte interrupt) => Run(instance, ((nint)text, interrupt != 0), static (backend, call) => backend.Output(Text(call.Item1), call.Item2));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Stop(void* instance) => Run(instance, static backend => backend.Stop());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Pause(void* instance) => Run(instance, static backend => backend.Pause());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Resume(void* instance) => Run(instance, static backend => backend.Resume());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError IsSpeaking(void* instance, byte* speaking) => Get(instance, speaking, static backend => backend.IsSpeaking() ? (byte)1 : (byte)0);

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetVolume(void* instance, float volume) => Run(instance, volume, static (backend, volume) => backend.SetVolume(volume));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVolume(void* instance, float* volume) => Get(instance, volume, static backend => backend.GetVolume());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetRate(void* instance, float rate) => Run(instance, rate, static (backend, rate) => backend.SetRate(rate));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetRate(void* instance, float* rate) => Get(instance, rate, static backend => backend.GetRate());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetPitch(void* instance, float pitch) => Run(instance, pitch, static (backend, pitch) => backend.SetPitch(pitch));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetPitch(void* instance, float* pitch) => Get(instance, pitch, static backend => backend.GetPitch());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError RefreshVoices(void* instance) => Run(instance, static backend => backend.RefreshVoices());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError CountVoices(void* instance, nuint* count) => Get(instance, count, static backend => checked((nuint)backend.CountVoices()));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoiceName(void* instance, nuint voiceId, byte** name) => GetText(instance, voiceId, name, static (backend, index) => backend.GetVoiceName(index));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoiceLanguage(void* instance, nuint voiceId, byte** language) => GetText(instance, voiceId, language, static (backend, index) => backend.GetVoiceLanguage(index));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetVoice(void* instance, nuint voiceId) => Run(instance, voiceId, static (backend, voiceId) => backend.SetVoice(checked((int)voiceId)));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoice(void* instance, nuint* voiceId) => Get(instance, voiceId, static backend => checked((nuint)backend.GetVoice()));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetChannels(void* instance, nuint* channels) => Get(instance, channels, static backend => checked((nuint)backend.GetChannels()));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetSampleRate(void* instance, nuint* sampleRate) => Get(instance, sampleRate, static backend => checked((nuint)backend.GetSampleRate()));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetBitDepth(void* instance, nuint* bitDepth) => Get(instance, bitDepth, static backend => checked((nuint)backend.GetBitDepth()));

	private static PrismError Run(void* instance, Action<CustomBackend> operation) {
		try {
			operation(Backend(instance));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	private static PrismError Run<T>(void* instance, T argument, Action<CustomBackend, T> operation) {
		try {
			operation(Backend(instance), argument);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	private static PrismError Get<T>(void* instance, T* result, Func<CustomBackend, T> operation) where T : unmanaged {
		try {
			*result = operation(Backend(instance));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	private static PrismError GetText(void* instance, nuint voiceId, byte** result, Func<CustomBackend, int, string> operation) {
		try {
			Instance state = Get(instance);
			*result = state.Hold(operation(state.Backend, checked((int)voiceId)));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	private static PrismError Failure(Exception error) {
		switch (error) {
			case PrismException prism:
				return prism.Error;
			case NotImplementedException or NotSupportedException:
				return PrismError.NotImplemented;
			// An index or count past what an int or prism's size type holds.
			case ArgumentOutOfRangeException or OverflowException:
				return PrismError.RangeOutOfBounds;
			case ArgumentException:
				return PrismError.InvalidParameter;
			case OutOfMemoryException:
				return PrismError.MemoryFailure;
			default:
				PrismLog.WriteFailure("A custom backend", error);
				return PrismError.Internal;
		}
	}

	private static bool Has(BackendFeatures features, BackendFeatures feature) => (features & feature) != 0;

	private static Instance Get(void* instance) => (Instance)GCHandle.FromIntPtr((nint)instance).Target!;

	private static CustomBackend Backend(void* instance) => Get(instance).Backend;

	// Decoded inside each operation's try, since even that can fail.
	private static string Text(nint text) => Utf8.Decode((byte*)text) ?? string.Empty;

	// prism only needs a voice string until the next name or language call on the same
	// instance, so one buffer per instance is reused and freed with it.
	private sealed class Instance(CustomBackend backend) {
		private byte* _text;

		public CustomBackend Backend { get; } = backend;

		public byte* Hold(string value) {
			byte[] bytes = Utf8.Encode(value, nameof(value));
			ReleaseText();
			_text = (byte*)NativeMemory.Alloc((nuint)bytes.Length);
			bytes.CopyTo(new Span<byte>(_text, bytes.Length));
			return _text;
		}

		public void ReleaseText() {
			NativeMemory.Free(_text);
			_text = null;
		}
	}
}
