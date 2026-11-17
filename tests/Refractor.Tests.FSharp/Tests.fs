// Refractor is written in C#, so these check that F# can use it the way F# code is written.
module Refractor.Tests.FSharp.Tests

open System
open System.Collections.Concurrent
open Refractor
open Xunit

type CollectingBackend() =
    inherit CustomBackend()
    member val Spoken = ResizeArray<string>()
    override this.Speak(text, _) = this.Spoken.Add text
    override _.SpeakToMemory(_, sink) = sink.Write([| 0.25f; -0.25f |], 1, 22050)

let collect (backend: Backend) text =
    let samples = ResizeArray<float32>()
    backend.SpeakToMemory(text, fun chunk _ _ -> samples.AddRange(chunk.ToArray()))
    List.ofSeq samples

[<Fact>]
let ``speaks and synthesizes through No Speech`` () =
    use prism = new Prism()
    use backend = prism.Create BackendId.NoSpeech
    backend.Initialize() |> ignore
    backend.Speak "Hello from F#"
    backend.Speak("Now", interrupt = true)
    Assert.NotEmpty(collect backend "Hello")

[<Fact>]
let ``finds backends by matching on the try pattern`` () =
    use prism = new Prism()
    match prism.TryFindBackend "No Speech" with
    | true, id -> Assert.Equal(BackendId.NoSpeech, id)
    | false, _ -> failwith "No Speech is not registered"

[<Fact>]
let ``a custom backend can speak to memory`` () =
    let custom = CollectingBackend()
    use builder = new RegistryBuilder()
    let id = builder.AddBackend("Collecting", 1, BackendFeatures.Speak ||| BackendFeatures.SpeakToMemory, fun () -> custom)
    use registry = builder.Freeze()
    use prism = new Prism(PrismOptions(Registry = registry))
    use backend = prism.Create id
    backend.Initialize() |> ignore
    backend.Speak "Hi"
    Assert.Equal<float32 list>([ 0.25f; -0.25f ], collect backend "Hi")
    Assert.Equal<string list>([ "Hi" ], List.ofSeq custom.Spoken)

[<Fact>]
let ``log messages arrive through the event`` () =
    let received = ConcurrentQueue<string>()
    let handler = EventHandler<LogMessageEventArgs>(fun _ e -> if e.Source = "FSharp" then received.Enqueue e.Message)
    let previous = PrismLog.SetLevel LogLevel.Trace
    PrismLog.MessageLogged.AddHandler handler
    try
        PrismLog.Write(LogLevel.Info, "FSharp", "Hello")
        PrismLog.Flush()
    finally
        PrismLog.MessageLogged.RemoveHandler handler
        PrismLog.SetLevel previous |> ignore
    Assert.Equal<string list>([ "Hello" ], List.ofSeq received)
