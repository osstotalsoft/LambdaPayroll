namespace LambdaPayroll.Api.HostedServices

open Microsoft.Extensions.Hosting
open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open FSharp.Control.Tasks.V2
open LambdaPayroll.Application
open System.Threading
open System.Threading.Tasks

type CompileDefinitions (interpreter: IInterpreter) =
    interface IHostedService with
        member _.StartAsync (ct: CancellationToken) =
            task {
                let effect = WriteApplication.sendCommand <| Compilation.Compile.Command ()
                do! interpreter.Interpret(effect |> Effect.unWrap |> EffectExtensions.ToUnit, ct)
            } :> Task
        member _.StopAsync (_ct: CancellationToken) = Task.CompletedTask



