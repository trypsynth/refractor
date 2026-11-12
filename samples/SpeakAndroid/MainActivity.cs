using Android.App;
using Android.OS;
using Android.Util;
using Refractor;

namespace SpeakAndroid;

// A fixed Java name, so it can be started from adb without knowing the generated one.
[Activity(Label = "Refractor", MainLauncher = true, Name = "com.trypsynth.refractor.speak.MainActivity")]
public sealed class MainActivity : Activity {
	private const string Tag = "REFRACTOR";
	private Prism? _prism;
	private Backend? _backend;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);
		// Starting Android's speech engine waits for a callback that arrives on this thread, so the
		// backend is made on another one.
		Task.Run(Speak);
	}

	private void Speak() {
		try {
			_prism = new Prism();
			Log.Info(Tag, $"prism {Prism.VersionString}");
			foreach (BackendId id in _prism.BackendIds) Log.Info(Tag, $"backend {_prism.GetBackendName(id)}");
			_backend = _prism.CreateBest();
			Log.Info(Tag, $"best {_backend.Name}");
			_backend.Speak("Hello from Refractor on Android.");
			Log.Info(Tag, "spoke");
		} catch (Exception error) {
			Log.Error(Tag, $"failed: {error}");
		}
	}
}
