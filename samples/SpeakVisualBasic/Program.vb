Imports System.Threading
Imports Refractor

Module Program
    Sub Main(args As String())
        AddHandler PrismLog.MessageLogged, Sub(sender, e) Console.Error.WriteLine($"{e.Level} {e.Source}: {e.Message}")
        PrismLog.SetLevel(LogLevel.Warning)

        Using prism As New Prism()
            Console.WriteLine($"prism {Prism.VersionString}")
            For Each id In prism.GetBackendIds()
                Console.WriteLine($"  {prism.GetBackendName(id)} (priority {prism.GetBackendPriority(id)})")
            Next
            If args.Length = 1 AndAlso args(0) = "--list" Then Return

            Using backend = prism.CreateBest()
                Console.WriteLine($"Speaking through {backend.Name}")
                backend.Speak(If(args.Length > 0, String.Join(" ", args), "Hello from Visual Basic."))
                If backend.Supports(BackendFeatures.IsSpeaking) Then
                    While backend.IsSpeaking
                        Thread.Sleep(50)
                    End While
                Else
                    Thread.Sleep(2000)
                End If
            End Using
        End Using
    End Sub
End Module
