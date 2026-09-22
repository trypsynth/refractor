using Android.App;
using Android.OS;
using Refractor;

namespace SpeakAndroid;

[Activity(Name = "com.trypsynth.refractor.speak.MainActivity", Label = "Refractor", MainLauncher = true)]
public sealed class MainActivity : Activity {
	private Prism? _prism;
	private Backend? _backend;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);
		// Android hands the speech engine's startup callback to the main thread, so prism has to wait for it on another one.
		Task.Run(Speak);
	}

	private void Speak() {
		try {
			_prism = new Prism();
			Log($"prism {Prism.VersionString}");
			foreach (BackendId id in _prism.BackendIds) Log($"backend {_prism.GetBackendName(id)}");
			_backend = _prism.CreateBest();
			Log($"best {_backend.Name}");
			_backend.Speak("Hello from Refractor on Android.");
			Log("spoke");
		} catch (Exception error) {
			Log($"failed {error}");
		}
	}

	private static void Log(string message) => Android.Util.Log.Info("REFRACTOR", message);
}
