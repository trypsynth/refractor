' Refractor is written in C#, so these check that Visual Basic can use it the way Visual Basic
' code is written.
Imports System.Collections.Concurrent
Imports Refractor
Imports Xunit

Public Class CollectingBackend
    Inherits CustomBackend

    Public ReadOnly Property Spoken As New List(Of String)

    Public Overrides Sub Speak(text As String, interrupt As Boolean)
        Spoken.Add(text)
    End Sub

    ' AudioSink was a ref struct, which Visual Basic cannot use, so this could not be written.
    Public Overrides Sub SpeakToMemory(text As String, sink As AudioSink)
        sink.Write(New Single() {0.25F, -0.25F}, 1, 22050)
    End Sub
End Class

Public Class Tests
    Private Shared Function Collect(backend As Backend, text As String) As List(Of Single)
        Dim samples As New List(Of Single)
        backend.SpeakToMemory(text, Sub(chunk, channels, sampleRate) samples.AddRange(chunk.ToArray()))
        Return samples
    End Function

    <Fact>
    Public Sub SpeaksAndSynthesizesThroughNoSpeech()
        Using prism As New Prism(), backend = prism.Create(BackendId.NoSpeech)
            backend.Initialize()
            backend.Speak("Hello from Visual Basic")
            backend.Speak("Now", interrupt:=True)
            Assert.NotEmpty(Collect(backend, "Hello"))
        End Using
    End Sub

    <Fact>
    Public Sub FindsBackendsByName()
        Using prism As New Prism()
            Dim id As BackendId
            Assert.True(prism.TryFindBackend("No Speech", id))
            Assert.Equal(BackendId.NoSpeech, id)
        End Using
    End Sub

    <Fact>
    Public Sub ACustomBackendCanSpeakToMemory()
        Dim custom As New CollectingBackend()
        Using builder As New RegistryBuilder()
            Dim id = builder.AddBackend("Collecting", 1, BackendFeatures.Speak Or BackendFeatures.SpeakToMemory, Function() custom)
            Using registry = builder.Freeze(), prism As New Prism(New PrismOptions With {.Registry = registry}), backend = prism.Create(id)
                backend.Initialize()
                backend.Speak("Hi")
                Assert.Equal({0.25F, -0.25F}, Collect(backend, "Hi"))
                Assert.Equal({"Hi"}, custom.Spoken)
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub LogMessagesArriveThroughTheEvent()
        Dim received As New ConcurrentQueue(Of String)
        Dim handler As EventHandler(Of LogMessageEventArgs) =
            Sub(sender, e)
                If e.Source = "VisualBasic" Then received.Enqueue(e.Message)
            End Sub
        Dim previous = PrismLog.SetLevel(LogLevel.Trace)
        AddHandler PrismLog.MessageLogged, handler
        Try
            PrismLog.Write(LogLevel.Info, "VisualBasic", "Hello")
            PrismLog.Flush()
        Finally
            RemoveHandler PrismLog.MessageLogged, handler
            PrismLog.SetLevel(previous)
        End Try
        Assert.Equal({"Hello"}, received)
    End Sub
End Class
