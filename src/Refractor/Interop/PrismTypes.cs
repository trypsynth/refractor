namespace Refractor.Interop;

public struct PrismContext;

public struct PrismBackend;

public struct PrismRegistry;

public struct PrismRegistryBuilder;

public unsafe struct PrismConfig {
	public byte Version;
	public PrismRegistry* Registry;
	public delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte, void> AvailabilityCallback;
	public void* AvailabilityUserdata;
	public uint AvailabilityPollIntervalMs;
	public uint AvailabilityDebounceSamples;
	public uint AvailabilityBackoffMaxMs;
	public byte AvailabilityAutoPowerManage;
	public delegate* unmanaged[Cdecl]<void*, void> AvailabilityBaselineCallback;
}

public unsafe struct PrismBackendVTable {
	public nuint Size;
	public delegate* unmanaged[Cdecl]<void*, void*> Create;
	public delegate* unmanaged[Cdecl]<void*, void> Destroy;
	public delegate* unmanaged[Cdecl]<void*, byte> IsSupported;
	public delegate* unmanaged[Cdecl]<void*, PrismError> Initialize;
	public delegate* unmanaged[Cdecl]<void*, byte*, byte, PrismError> Speak;
	public delegate* unmanaged[Cdecl]<void*, byte*, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void>, void*, PrismError> SpeakToMemory;
	public delegate* unmanaged[Cdecl]<void*, byte*, PrismError> Braille;
	public delegate* unmanaged[Cdecl]<void*, byte*, byte, PrismError> Output;
	public delegate* unmanaged[Cdecl]<void*, PrismError> Stop;
	public delegate* unmanaged[Cdecl]<void*, PrismError> Pause;
	public delegate* unmanaged[Cdecl]<void*, PrismError> Resume;
	public delegate* unmanaged[Cdecl]<void*, byte*, PrismError> IsSpeaking;
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetVolume;
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetVolume;
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetRate;
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetRate;
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetPitch;
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetPitch;
	public delegate* unmanaged[Cdecl]<void*, PrismError> RefreshVoices;
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> CountVoices;
	public delegate* unmanaged[Cdecl]<void*, nuint, byte**, PrismError> GetVoiceName;
	public delegate* unmanaged[Cdecl]<void*, nuint, byte**, PrismError> GetVoiceLanguage;
	public delegate* unmanaged[Cdecl]<void*, nuint, PrismError> SetVoice;
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetVoice;
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetChannels;
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetSampleRate;
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetBitDepth;
}

public unsafe struct PrismLogHandler {
	public delegate* unmanaged[Cdecl]<void*, LogLevel, byte*, byte*, void> Callback;
	public void* Userdata;
}

public unsafe struct PrismPluginServices {
	public uint StructSize;
	public uint Reserved;
	public delegate* unmanaged[Cdecl]<PrismPluginServices*, LogLevel, byte*, void> Log;
}

public unsafe struct PrismPluginInstanceContext {
	public uint StructSize;
	public uint Reserved;
	public PrismPluginServices* Services;
	public void* Userdata;
}

public unsafe struct PrismPluginHost {
	public ulong AbiVersion;
	public uint StructSize;
	public uint Reserved;
	public delegate* unmanaged[Cdecl]<PrismPluginHost*, LogLevel, byte*, void> Log;
}

public unsafe struct PrismPluginBackend {
	public ulong AbiVersion;
	public uint StructSize;
	public uint Reserved;
	public byte* Name;
	public int Priority;
	public ulong Features;
	public PrismBackendVTable* VTable;
	public void* Userdata;
	public ulong PluginVersion;
}
