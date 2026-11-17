# F# and Visual Basic

Refractor is written in C#, but works the same from F# and Visual Basic, custom backends included. Its tests run in all three languages.

## F#

```fsharp
open Refractor

use prism = new Prism()
use backend = prism.CreateBest()
backend.Speak "Hello from F#"
```

<xref:Refractor.Prism.TryFindBackend*> reads as a tuple to match on:

```fsharp
match prism.TryFindBackend "NVDA" with
| true, id -> printfn "NVDA is %O" id
| false, _ -> printfn "NVDA is not registered"
```

Synthesizing to memory takes a lambda, and a custom backend is an ordinary subclass:

```fsharp
let samples = ResizeArray<float32>()
backend.SpeakToMemory("Hello", fun chunk channels sampleRate -> samples.AddRange(chunk.ToArray()))

type ConsoleBackend() =
    inherit CustomBackend()
    override _.Speak(text, _) = printfn "%s" text

use builder = new RegistryBuilder()
let id = builder.AddBackend("Console", 100, BackendFeatures.Speak, fun () -> ConsoleBackend())
```

Log messages are an event:

```fsharp
let handler = EventHandler<LogMessageEventArgs>(fun _ e -> eprintfn "%O %s" e.Level e.Message)
PrismLog.MessageLogged.AddHandler handler
```

## Visual Basic

```vb
Imports Refractor

Using prism As New Prism(), backend = prism.CreateBest()
    backend.Speak("Hello from Visual Basic")
End Using
```

Events and custom backends work the usual Visual Basic way:

```vb
AddHandler PrismLog.MessageLogged, Sub(sender, e) Console.Error.WriteLine($"{e.Level} {e.Message}")

Public Class ConsoleBackend
    Inherits CustomBackend

    Public Overrides Sub Speak(text As String, interrupt As Boolean)
        Console.WriteLine(text)
    End Sub

    Public Overrides Sub SpeakToMemory(text As String, sink As AudioSink)
        sink.Write(New Single() {0F, 0F}, 1, 22050)
    End Sub
End Class
```

## Samples

[`samples/SpeakFSharp`](https://github.com/trypsynth/refractor/tree/master/samples/SpeakFSharp) and [`samples/SpeakVisualBasic`](https://github.com/trypsynth/refractor/tree/master/samples/SpeakVisualBasic) are complete programs that list the backends and speak through the best one.
