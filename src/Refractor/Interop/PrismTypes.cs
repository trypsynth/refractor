namespace Refractor.Interop;

/// <summary>An opaque prism context.</summary>
public struct PrismContext;

/// <summary>An opaque backend handle.</summary>
public struct PrismBackend;

/// <summary>An opaque frozen registry.</summary>
public struct PrismRegistry;

/// <summary>An opaque registry builder.</summary>
public struct PrismRegistryBuilder;

/// <summary>The settings <c>prism_init</c> takes. Start from <see cref="PrismNative.ConfigInit"/>.</summary>
public unsafe struct PrismConfig {
	/// <summary>The C field <c>version</c>.</summary>
	public byte Version;
	/// <summary>The C field <c>registry</c>.</summary>
	public PrismRegistry* Registry;
	/// <summary>The C field <c>availability_callback</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte, void> AvailabilityCallback;
	/// <summary>The C field <c>availability_userdata</c>.</summary>
	public void* AvailabilityUserdata;
	/// <summary>The C field <c>availability_poll_interval_ms</c>.</summary>
	public uint AvailabilityPollIntervalMs;
	/// <summary>The C field <c>availability_debounce_samples</c>.</summary>
	public uint AvailabilityDebounceSamples;
	/// <summary>The C field <c>availability_backoff_max_ms</c>.</summary>
	public uint AvailabilityBackoffMaxMs;
	/// <summary>The C field <c>availability_auto_power_manage</c>.</summary>
	public byte AvailabilityAutoPowerManage;
	/// <summary>The C field <c>availability_baseline_callback</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, void> AvailabilityBaselineCallback;
}

/// <summary>The operations of a backend registered from outside prism. A null entry is an operation it does not have.</summary>
public unsafe struct PrismBackendVTable {
	/// <summary>The C field <c>size</c>.</summary>
	public nuint Size;
	/// <summary>The C field <c>create</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, void*> Create;
	/// <summary>The C field <c>destroy</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, void> Destroy;
	/// <summary>The C field <c>is_supported</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte> IsSupported;
	/// <summary>The C field <c>initialize</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, PrismError> Initialize;
	/// <summary>The C field <c>speak</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte*, byte, PrismError> Speak;
	/// <summary>The C field <c>speak_to_memory</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte*, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void>, void*, PrismError> SpeakToMemory;
	/// <summary>The C field <c>braille</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte*, PrismError> Braille;
	/// <summary>The C field <c>output</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte*, byte, PrismError> Output;
	/// <summary>The C field <c>stop</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, PrismError> Stop;
	/// <summary>The C field <c>pause</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, PrismError> Pause;
	/// <summary>The C field <c>resume</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, PrismError> Resume;
	/// <summary>The C field <c>is_speaking</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, byte*, PrismError> IsSpeaking;
	/// <summary>The C field <c>set_volume</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetVolume;
	/// <summary>The C field <c>get_volume</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetVolume;
	/// <summary>The C field <c>set_rate</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetRate;
	/// <summary>The C field <c>get_rate</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetRate;
	/// <summary>The C field <c>set_pitch</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float, PrismError> SetPitch;
	/// <summary>The C field <c>get_pitch</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, float*, PrismError> GetPitch;
	/// <summary>The C field <c>refresh_voices</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, PrismError> RefreshVoices;
	/// <summary>The C field <c>count_voices</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> CountVoices;
	/// <summary>The C field <c>get_voice_name</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint, byte**, PrismError> GetVoiceName;
	/// <summary>The C field <c>get_voice_language</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint, byte**, PrismError> GetVoiceLanguage;
	/// <summary>The C field <c>set_voice</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint, PrismError> SetVoice;
	/// <summary>The C field <c>get_voice</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetVoice;
	/// <summary>The C field <c>get_channels</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetChannels;
	/// <summary>The C field <c>get_sample_rate</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetSampleRate;
	/// <summary>The C field <c>get_bit_depth</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, nuint*, PrismError> GetBitDepth;
}

/// <summary>A log callback and its user data.</summary>
public unsafe struct PrismLogHandler {
	/// <summary>The C field <c>fn</c>.</summary>
	public delegate* unmanaged[Cdecl]<void*, LogLevel, byte*, byte*, void> Callback;
	/// <summary>The C field <c>userdata</c>.</summary>
	public void* Userdata;
}

/// <summary>What prism offers a plugin backend's instances.</summary>
public unsafe struct PrismPluginServices {
	/// <summary>The C field <c>struct_size</c>.</summary>
	public uint StructSize;
	/// <summary>The C field <c>reserved</c>.</summary>
	public uint Reserved;
	/// <summary>The C field <c>log</c>.</summary>
	public delegate* unmanaged[Cdecl]<PrismPluginServices*, LogLevel, byte*, void> Log;
}

/// <summary>What a plugin backend's instance receives when it is created.</summary>
public unsafe struct PrismPluginInstanceContext {
	/// <summary>The C field <c>struct_size</c>.</summary>
	public uint StructSize;
	/// <summary>The C field <c>reserved</c>.</summary>
	public uint Reserved;
	/// <summary>The C field <c>services</c>.</summary>
	public PrismPluginServices* Services;
	/// <summary>The C field <c>userdata</c>.</summary>
	public void* Userdata;
}

/// <summary>What prism offers a plugin library while it is queried.</summary>
public unsafe struct PrismPluginHost {
	/// <summary>The C field <c>abi_version</c>.</summary>
	public ulong AbiVersion;
	/// <summary>The C field <c>struct_size</c>.</summary>
	public uint StructSize;
	/// <summary>The C field <c>reserved</c>.</summary>
	public uint Reserved;
	/// <summary>The C field <c>log</c>.</summary>
	public delegate* unmanaged[Cdecl]<PrismPluginHost*, LogLevel, byte*, void> Log;
}

/// <summary>One backend a plugin library declares.</summary>
public unsafe struct PrismPluginBackend {
	/// <summary>The C field <c>abi_version</c>.</summary>
	public ulong AbiVersion;
	/// <summary>The C field <c>struct_size</c>.</summary>
	public uint StructSize;
	/// <summary>The C field <c>reserved</c>.</summary>
	public uint Reserved;
	/// <summary>The C field <c>name</c>.</summary>
	public byte* Name;
	/// <summary>The C field <c>priority</c>.</summary>
	public int Priority;
	/// <summary>The C field <c>features</c>.</summary>
	public ulong Features;
	/// <summary>The C field <c>vtable</c>.</summary>
	public PrismBackendVTable* VTable;
	/// <summary>The C field <c>userdata</c>.</summary>
	public void* Userdata;
	/// <summary>The C field <c>plugin_version</c>.</summary>
	public ulong PluginVersion;
}
