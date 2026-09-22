using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Refractor.Interop;

namespace Refractor;

// Every entry point catches everything, because an exception cannot unwind back through prism.
internal static unsafe class CustomBackendBridge {
	private const string LogSource = "Refractor";

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

	private static bool Has(BackendFeatures features, BackendFeatures feature) => (features & feature) != 0;

	private static Instance Get(void* instance) => (Instance)GCHandle.FromIntPtr((nint)instance).Target!;

	private static CustomBackend Backend(void* instance) => Get(instance).Backend;

	private static string Text(byte* text) => Utf8.Decode(text) ?? string.Empty;

	private static PrismError Failure(Exception error) {
		switch (error) {
			case PrismException prism:
				return prism.Error;
			case NotImplementedException or NotSupportedException:
				return PrismError.NotImplemented;
			case ArgumentOutOfRangeException:
				return PrismError.RangeOutOfBounds;
			case ArgumentException:
				return PrismError.InvalidParameter;
			case OutOfMemoryException:
				return PrismError.MemoryFailure;
			default:
				Log(error);
				return PrismError.Internal;
		}
	}

	private static void Log(Exception error) {
		byte[] source = Encoding.UTF8.GetBytes(LogSource + "\0");
		byte[] message = Encoding.UTF8.GetBytes($"A custom backend threw {error.GetType().FullName}: {error.Message}".Replace('\0', ' ') + "\0");
		fixed (byte* sourceText = source)
		fixed (byte* messageText = message) {
			PrismNative.Log(LogLevel.Error, sourceText, messageText);
		}
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
			Log(error);
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
			Log(error);
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
			Log(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Initialize(void* instance) {
		try {
			Backend(instance).Initialize();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Speak(void* instance, byte* text, byte interrupt) {
		try {
			Backend(instance).Speak(Text(text), interrupt != 0);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SpeakToMemory(void* instance, byte* text, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata) {
		try {
			Backend(instance).SpeakToMemory(Text(text), new AudioSink(callback, userdata));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Braille(void* instance, byte* text) {
		try {
			Backend(instance).Braille(Text(text));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Output(void* instance, byte* text, byte interrupt) {
		try {
			Backend(instance).Output(Text(text), interrupt != 0);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Stop(void* instance) {
		try {
			Backend(instance).Stop();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Pause(void* instance) {
		try {
			Backend(instance).Pause();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError Resume(void* instance) {
		try {
			Backend(instance).Resume();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError IsSpeaking(void* instance, byte* speaking) {
		try {
			*speaking = Backend(instance).IsSpeaking() ? (byte)1 : (byte)0;
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetVolume(void* instance, float volume) {
		try {
			Backend(instance).SetVolume(volume);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVolume(void* instance, float* volume) {
		try {
			*volume = Backend(instance).GetVolume();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetRate(void* instance, float rate) {
		try {
			Backend(instance).SetRate(rate);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetRate(void* instance, float* rate) {
		try {
			*rate = Backend(instance).GetRate();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetPitch(void* instance, float pitch) {
		try {
			Backend(instance).SetPitch(pitch);
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetPitch(void* instance, float* pitch) {
		try {
			*pitch = Backend(instance).GetPitch();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError RefreshVoices(void* instance) {
		try {
			Backend(instance).RefreshVoices();
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError CountVoices(void* instance, nuint* count) {
		try {
			*count = checked((nuint)Backend(instance).CountVoices());
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoiceName(void* instance, nuint voiceId, byte** name) {
		try {
			Instance state = Get(instance);
			*name = state.Hold(state.Backend.GetVoiceName(checked((int)voiceId)));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoiceLanguage(void* instance, nuint voiceId, byte** language) {
		try {
			Instance state = Get(instance);
			*language = state.Hold(state.Backend.GetVoiceLanguage(checked((int)voiceId)));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError SetVoice(void* instance, nuint voiceId) {
		try {
			Backend(instance).SetVoice(checked((int)voiceId));
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetVoice(void* instance, nuint* voiceId) {
		try {
			*voiceId = checked((nuint)Backend(instance).GetVoice());
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetChannels(void* instance, nuint* channels) {
		try {
			*channels = checked((nuint)Backend(instance).GetChannels());
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetSampleRate(void* instance, nuint* sampleRate) {
		try {
			*sampleRate = checked((nuint)Backend(instance).GetSampleRate());
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static PrismError GetBitDepth(void* instance, nuint* bitDepth) {
		try {
			*bitDepth = checked((nuint)Backend(instance).GetBitDepth());
			return PrismError.Ok;
		} catch (Exception error) {
			return Failure(error);
		}
	}

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
