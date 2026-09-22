using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Refractor.Interop;

/// <summary>The prism C API, one to one. <see cref="Prism"/> and the types around it wrap it safely.</summary>
public static unsafe partial class PrismNative {
	public const string Library = "prism";
	public const byte ConfigVersion = 4;
	public const ulong PluginAbiVersion = 1;
	public const int ErrorCount = 24;
	public const ulong FeatureMaxBit = 1UL << 63;
	public const string PluginEntryPoint = "prism_plugin_query";

	// An iOS app has prism linked into its own executable, since it cannot load a library of its own.
	static PrismNative() {
		if (OperatingSystem.IsIOS()) NativeLibrary.SetDllImportResolver(typeof(PrismNative).Assembly, ResolveFromApp);
	}

	[LibraryImport(Library, EntryPoint = "prism_config_init")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismConfig ConfigInit();

	[LibraryImport(Library, EntryPoint = "prism_init")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismContext* Init(PrismConfig* config);

	[LibraryImport(Library, EntryPoint = "prism_shutdown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void Shutdown(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_availability_poll_pause")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AvailabilityPollPause(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_availability_poll_resume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AvailabilityPollResume(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_availability_auto_power_supported")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool AvailabilityAutoPowerSupported();

	[LibraryImport(Library, EntryPoint = "prism_registry_count")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nuint RegistryCount(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_registry_id_at")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong RegistryIdAt(PrismContext* context, nuint index);

	[LibraryImport(Library, EntryPoint = "prism_registry_id")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong RegistryId(PrismContext* context, byte* name);

	[LibraryImport(Library, EntryPoint = "prism_registry_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* RegistryName(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_priority")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int RegistryPriority(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_exists")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool RegistryExists(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_get")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryGet(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_create")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryCreate(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_create_best")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryCreateBest(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_registry_acquire")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryAcquire(PrismContext* context, ulong id);

	[LibraryImport(Library, EntryPoint = "prism_registry_acquire_best")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryAcquireBest(PrismContext* context);

	[LibraryImport(Library, EntryPoint = "prism_backend_free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void BackendFree(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* BackendName(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_features")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong BackendGetFeatures(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_initialize")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendInitialize(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_speak")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSpeak(PrismBackend* backend, byte* text, [MarshalAs(UnmanagedType.U1)] bool interrupt);

	[LibraryImport(Library, EntryPoint = "prism_backend_speak_to_memory")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSpeakToMemory(PrismBackend* backend, byte* text, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata);

	[LibraryImport(Library, EntryPoint = "prism_backend_braille")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendBraille(PrismBackend* backend, byte* text);

	[LibraryImport(Library, EntryPoint = "prism_backend_output")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendOutput(PrismBackend* backend, byte* text, [MarshalAs(UnmanagedType.U1)] bool interrupt);

	[LibraryImport(Library, EntryPoint = "prism_backend_stop")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendStop(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_pause")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendPause(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_resume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendResume(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_is_speaking")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendIsSpeaking(PrismBackend* backend, byte* speaking);

	[LibraryImport(Library, EntryPoint = "prism_backend_set_volume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetVolume(PrismBackend* backend, float volume);

	[LibraryImport(Library, EntryPoint = "prism_backend_set_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetRate(PrismBackend* backend, float rate);

	[LibraryImport(Library, EntryPoint = "prism_backend_set_pitch")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetPitch(PrismBackend* backend, float pitch);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_volume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVolume(PrismBackend* backend, float* volume);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetRate(PrismBackend* backend, float* rate);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_pitch")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetPitch(PrismBackend* backend, float* pitch);

	[LibraryImport(Library, EntryPoint = "prism_backend_refresh_voices")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendRefreshVoices(PrismBackend* backend);

	[LibraryImport(Library, EntryPoint = "prism_backend_count_voices")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendCountVoices(PrismBackend* backend, nuint* count);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoiceName(PrismBackend* backend, nuint voiceId, byte** name);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice_language")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoiceLanguage(PrismBackend* backend, nuint voiceId, byte** language);

	[LibraryImport(Library, EntryPoint = "prism_backend_set_voice")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetVoice(PrismBackend* backend, nuint voiceId);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoice(PrismBackend* backend, nuint* voiceId);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_channels")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetChannels(PrismBackend* backend, nuint* channels);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_sample_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetSampleRate(PrismBackend* backend, nuint* sampleRate);

	[LibraryImport(Library, EntryPoint = "prism_backend_get_bit_depth")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetBitDepth(PrismBackend* backend, nuint* bitDepth);

	[LibraryImport(Library, EntryPoint = "prism_error_string")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* ErrorString(PrismError error);

	[LibraryImport(Library, EntryPoint = "prism_registry_builder_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistryBuilder* RegistryBuilderNew();

	[LibraryImport(Library, EntryPoint = "prism_registry_builder_add_backend")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError RegistryBuilderAddBackend(PrismRegistryBuilder* builder, byte* name, int priority, ulong features, PrismBackendVTable* vtable, void* userdata, delegate* unmanaged[Cdecl]<void*, void> userdataFree, ulong* id);

	[LibraryImport(Library, EntryPoint = "prism_registry_builder_add_library")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError RegistryBuilderAddLibrary(PrismRegistryBuilder* builder, byte* path, int priorityOverride, nuint* count);

	[LibraryImport(Library, EntryPoint = "prism_registry_freeze")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistry* RegistryFreeze(PrismRegistryBuilder* builder);

	[LibraryImport(Library, EntryPoint = "prism_registry_builder_free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void RegistryBuilderFree(PrismRegistryBuilder* builder);

	[LibraryImport(Library, EntryPoint = "prism_registry_retain")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistry* RegistryRetain(PrismRegistry* registry);

	[LibraryImport(Library, EntryPoint = "prism_registry_release")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void RegistryRelease(PrismRegistry* registry);

	[LibraryImport(Library, EntryPoint = "prism_set_log_handler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismLogHandler SetLogHandler(PrismLogHandler handler);

	[LibraryImport(Library, EntryPoint = "prism_set_log_level")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial LogLevel SetLogLevel(LogLevel level);

	[LibraryImport(Library, EntryPoint = "prism_log")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void Log(LogLevel level, byte* source, byte* message);

	[LibraryImport(Library, EntryPoint = "prism_log_flush")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void LogFlush();

	[LibraryImport(Library, EntryPoint = "prism_log_shutdown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void LogShutdown();

	[LibraryImport(Library, EntryPoint = "prism_version")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial uint Version();

	[LibraryImport(Library, EntryPoint = "prism_version_string")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* VersionString();

	private static nint ResolveFromApp(string name, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) =>
		name == Library ? NativeLibrary.GetMainProgramHandle() : 0;
}
