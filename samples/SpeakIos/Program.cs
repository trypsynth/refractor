using Foundation;
using Refractor;
using UIKit;

UIApplication.Main(args, null, typeof(AppDelegate));

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate {
	private Prism? _prism;
	private Backend? _backend;

	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions) {
		Window = new UIWindow(UIScreen.MainScreen.Bounds) { RootViewController = new UIViewController() };
		Window.MakeKeyAndVisible();
		Console.WriteLine($"REFRACTOR diag iOS={OperatingSystem.IsIOS()}");
		try {
			nint main = System.Runtime.InteropServices.NativeLibrary.GetMainProgramHandle();
			Console.WriteLine($"REFRACTOR diag main={main}");
			Console.WriteLine($"REFRACTOR diag export={System.Runtime.InteropServices.NativeLibrary.TryGetExport(main, "prism_version", out nint address)} {address}");
		} catch (Exception error) {
			Console.WriteLine($"REFRACTOR diag failed {error.GetType().Name}: {error.Message}");
		}
		_prism = new Prism();
		Console.WriteLine($"REFRACTOR prism {Prism.VersionString}");
		foreach (BackendId id in _prism.BackendIds) Console.WriteLine($"REFRACTOR backend {_prism.GetBackendName(id)}");
		_backend = _prism.CreateBest();
		Console.WriteLine($"REFRACTOR best {_backend.Name}");
		_backend.Speak("Hello from Refractor on iOS.");
		Console.WriteLine("REFRACTOR spoke");
		NSTimer.CreateScheduledTimer(3, _ => Environment.Exit(0));
		return true;
	}
}
