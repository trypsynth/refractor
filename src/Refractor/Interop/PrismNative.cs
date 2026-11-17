using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Refractor.Interop;

/// <summary>The prism C API, one to one. <see cref="Prism"/> and the types around it wrap it safely.</summary>
public static unsafe partial class PrismNative {
	/// <summary>The library name every import above loads: prism.dll, libprism.dylib or libprism.so.</summary>
	public const string Library = "prism";
	/// <summary><c>PRISM_CONFIG_VERSION</c>, the <see cref="PrismConfig.Version"/> this prism expects.</summary>
	public const byte ConfigVersion = 4;
	/// <summary><c>PRISM_PLUGIN_ABI_VERSION</c>, the plugin ABI this prism loads.</summary>
	public const ulong PluginAbiVersion = 1;
	/// <summary><c>PRISM_ERROR_COUNT</c>, the number of <see cref="PrismError"/> codes.</summary>
	public const int ErrorCount = 24;
	/// <summary><c>PRISM_BACKEND_FEATURE_MAX_BIT</c>, the highest feature bit prism reserves.</summary>
	public const ulong FeatureMaxBit = 1UL << 63;
	/// <summary>The function a prism plugin library exports for prism to find its backends.</summary>
	public const string PluginEntryPoint = "prism_plugin_query";

	// An iOS app has prism linked into its own executable, since it cannot load a library of its own.
	// On Android, Java loads prism first, so .NET finds it already loaded.
	[ModuleInitializer]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Mono looks prism up while compiling the first caller, before a static constructor here would run.")]
	internal static void RegisterResolver() {
		if (OperatingSystem.IsIOS()) NativeLibrary.SetDllImportResolver(typeof(PrismNative).Assembly, ResolveFromApp);
#if ANDROID
		LoadThroughJava();
#endif
	}

#if ANDROID
	// prism learns the Java VM from its JNI_OnLoad, which Android only runs for a library Java
	// loads, and which finds prism's Java classes through the loading class's class loader. A
	// thread .NET started sees only the boot class loader, so the app's own is asked both where
	// prism is and for PrismLoader, a class of Refractor's that does the loading.
	private static void LoadThroughJava() {
		Java.Lang.ClassLoader loader = Android.App.Application.Context.ClassLoader ?? throw new DllNotFoundException("The app has no class loader to find prism with.");
		string path = Android.Runtime.Extensions.JavaCast<Dalvik.SystemInterop.BaseDexClassLoader>(loader)?.FindLibrary(Library)
			?? throw new DllNotFoundException($"The app does not contain lib{Library}.so for this device's ABI.");
		using Java.Lang.Class prismLoader = loader.LoadClass("com.trypsynth.refractor.PrismLoader")!;
		nint load = Android.Runtime.JNIEnv.GetStaticMethodID(prismLoader.Handle, "load", "(Ljava/lang/String;)V");
		using Java.Lang.String text = new(path);
		Android.Runtime.JNIEnv.CallStaticVoidMethod(prismLoader.Handle, load, new Android.Runtime.JValue(text));
	}
#endif

	/// <summary>Calls <c>prism_config_init</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_config_init")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismConfig ConfigInit();

	/// <summary>Calls <c>prism_init</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_init")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismContext* Init(PrismConfig* config);

	/// <summary>Calls <c>prism_shutdown</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_shutdown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void Shutdown(PrismContext* context);

	/// <summary>Calls <c>prism_availability_poll_pause</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_availability_poll_pause")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AvailabilityPollPause(PrismContext* context);

	/// <summary>Calls <c>prism_availability_poll_resume</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_availability_poll_resume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AvailabilityPollResume(PrismContext* context);

	/// <summary>Calls <c>prism_availability_auto_power_supported</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_availability_auto_power_supported")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool AvailabilityAutoPowerSupported();

	/// <summary>Calls <c>prism_registry_count</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_count")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nuint RegistryCount(PrismContext* context);

	/// <summary>Calls <c>prism_registry_id_at</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_id_at")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong RegistryIdAt(PrismContext* context, nuint index);

	/// <summary>Calls <c>prism_registry_id</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_id")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong RegistryId(PrismContext* context, byte* name);

	/// <summary>Calls <c>prism_registry_name</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* RegistryName(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_priority</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_priority")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int RegistryPriority(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_exists</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_exists")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool RegistryExists(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_get</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_get")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryGet(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_create</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_create")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryCreate(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_create_best</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_create_best")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryCreateBest(PrismContext* context);

	/// <summary>Calls <c>prism_registry_acquire</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_acquire")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryAcquire(PrismContext* context, ulong id);

	/// <summary>Calls <c>prism_registry_acquire_best</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_acquire_best")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismBackend* RegistryAcquireBest(PrismContext* context);

	/// <summary>Calls <c>prism_backend_free</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void BackendFree(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_name</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* BackendName(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_get_features</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_features")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial ulong BackendGetFeatures(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_initialize</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_initialize")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendInitialize(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_speak</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_speak")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSpeak(PrismBackend* backend, byte* text, [MarshalAs(UnmanagedType.U1)] bool interrupt);

	/// <summary>Calls <c>prism_backend_speak_to_memory</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_speak_to_memory")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSpeakToMemory(PrismBackend* backend, byte* text, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback, void* userdata);

	/// <summary>Calls <c>prism_backend_braille</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_braille")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendBraille(PrismBackend* backend, byte* text);

	/// <summary>Calls <c>prism_backend_output</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_output")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendOutput(PrismBackend* backend, byte* text, [MarshalAs(UnmanagedType.U1)] bool interrupt);

	/// <summary>Calls <c>prism_backend_stop</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_stop")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendStop(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_pause</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_pause")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendPause(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_resume</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_resume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendResume(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_is_speaking</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_is_speaking")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendIsSpeaking(PrismBackend* backend, byte* speaking);

	/// <summary>Calls <c>prism_backend_set_volume</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_set_volume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetVolume(PrismBackend* backend, float volume);

	/// <summary>Calls <c>prism_backend_set_rate</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_set_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetRate(PrismBackend* backend, float rate);

	/// <summary>Calls <c>prism_backend_set_pitch</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_set_pitch")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetPitch(PrismBackend* backend, float pitch);

	/// <summary>Calls <c>prism_backend_get_volume</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_volume")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVolume(PrismBackend* backend, float* volume);

	/// <summary>Calls <c>prism_backend_get_rate</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetRate(PrismBackend* backend, float* rate);

	/// <summary>Calls <c>prism_backend_get_pitch</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_pitch")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetPitch(PrismBackend* backend, float* pitch);

	/// <summary>Calls <c>prism_backend_refresh_voices</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_refresh_voices")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendRefreshVoices(PrismBackend* backend);

	/// <summary>Calls <c>prism_backend_count_voices</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_count_voices")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendCountVoices(PrismBackend* backend, nuint* count);

	/// <summary>Calls <c>prism_backend_get_voice_name</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice_name")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoiceName(PrismBackend* backend, nuint voiceId, byte** name);

	/// <summary>Calls <c>prism_backend_get_voice_language</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice_language")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoiceLanguage(PrismBackend* backend, nuint voiceId, byte** language);

	/// <summary>Calls <c>prism_backend_set_voice</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_set_voice")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendSetVoice(PrismBackend* backend, nuint voiceId);

	/// <summary>Calls <c>prism_backend_get_voice</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_voice")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetVoice(PrismBackend* backend, nuint* voiceId);

	/// <summary>Calls <c>prism_backend_get_channels</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_channels")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetChannels(PrismBackend* backend, nuint* channels);

	/// <summary>Calls <c>prism_backend_get_sample_rate</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_sample_rate")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetSampleRate(PrismBackend* backend, nuint* sampleRate);

	/// <summary>Calls <c>prism_backend_get_bit_depth</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_backend_get_bit_depth")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError BackendGetBitDepth(PrismBackend* backend, nuint* bitDepth);

	/// <summary>Calls <c>prism_error_string</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_error_string")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* ErrorString(PrismError error);

	/// <summary>Calls <c>prism_registry_builder_new</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_builder_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistryBuilder* RegistryBuilderNew();

	/// <summary>Calls <c>prism_registry_builder_add_backend</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_builder_add_backend")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError RegistryBuilderAddBackend(PrismRegistryBuilder* builder, byte* name, int priority, ulong features, PrismBackendVTable* vtable, void* userdata, delegate* unmanaged[Cdecl]<void*, void> userdataFree, ulong* id);

	/// <summary>Calls <c>prism_registry_builder_add_library</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_builder_add_library")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismError RegistryBuilderAddLibrary(PrismRegistryBuilder* builder, byte* path, int priorityOverride, nuint* count);

	/// <summary>Calls <c>prism_registry_freeze</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_freeze")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistry* RegistryFreeze(PrismRegistryBuilder* builder);

	/// <summary>Calls <c>prism_registry_builder_free</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_builder_free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void RegistryBuilderFree(PrismRegistryBuilder* builder);

	/// <summary>Calls <c>prism_registry_retain</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_retain")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismRegistry* RegistryRetain(PrismRegistry* registry);

	/// <summary>Calls <c>prism_registry_release</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_registry_release")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void RegistryRelease(PrismRegistry* registry);

	/// <summary>Calls <c>prism_set_log_handler</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_set_log_handler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial PrismLogHandler SetLogHandler(PrismLogHandler handler);

	/// <summary>Calls <c>prism_set_log_level</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_set_log_level")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial LogLevel SetLogLevel(LogLevel level);

	/// <summary>Calls <c>prism_log</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_log")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void Log(LogLevel level, byte* source, byte* message);

	/// <summary>Calls <c>prism_log_flush</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_log_flush")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void LogFlush();

	/// <summary>Calls <c>prism_log_shutdown</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_log_shutdown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void LogShutdown();

	/// <summary>Calls <c>prism_version</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_version")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial uint Version();

	/// <summary>Calls <c>prism_version_string</c>.</summary>
	[LibraryImport(Library, EntryPoint = "prism_version_string")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* VersionString();

	// Mono only accepts a handle it loaded itself, which rules out GetMainProgramHandle's pseudo handle.
	private static nint ResolveFromApp(string name, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) =>
		name == Library ? NativeLibrary.Load(Environment.ProcessPath!) : 0;
}
