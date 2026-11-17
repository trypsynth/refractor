open System
open System.Threading
open Refractor

[<EntryPoint>]
let main args =
    use prism = new Prism()
    printfn "prism %s" Prism.VersionString
    for id in prism.GetBackendIds() do
        printfn "  %s (priority %d)" (prism.GetBackendName id) ((prism.GetBackendPriority id).GetValueOrDefault())

    if args = [| "--list" |] then
        0
    else
        use backend = prism.CreateBest()
        printfn "Speaking through %s" backend.Name
        backend.Speak(if args.Length > 0 then String.Join(' ', args) else "Hello from F#.")
        if backend.Supports BackendFeatures.IsSpeaking then
            while backend.IsSpeaking do
                Thread.Sleep 50
        else
            Thread.Sleep 2000
        0
