using Refractor;

using Prism prism = new();
Console.WriteLine($"prism {Prism.VersionString}");
foreach (BackendId id in prism.BackendIds) Console.WriteLine($"  {prism.GetBackendName(id)} (priority {prism.GetBackendPriority(id)})");
using Backend backend = prism.CreateBest();
Console.WriteLine($"Speaking through {backend.Name}");
backend.Speak(args.Length > 0 ? string.Join(' ', args) : "Hello from Refractor.");
if (backend.Supports(BackendFeatures.IsSpeaking)) {
	while (backend.IsSpeaking) Thread.Sleep(50);
} else {
	Thread.Sleep(2000);
}
