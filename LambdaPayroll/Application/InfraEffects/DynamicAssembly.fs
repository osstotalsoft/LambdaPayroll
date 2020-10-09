namespace LambdaPayroll.Application.InfraEffects

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open System.Reflection
open ElemAlgebra

type DynamicAssembly = DynamicAssembly of Assembly

module DynamicAssemblyService =
    type Range =
        { StartLine: int
          EndLine: int
          StartColumn: int
          EndColumn: int }

    type ErrorInfo =
        { Message: string
          Severity: string
          Range: Range }

    module ErrorInfo =
        let format: (ErrorInfo list -> string) =
            Seq.map (fun e ->
                sprintf
                    "%s: (%i,%i-%i,%i) %s"
                    e.Severity
                    e.Range.StartLine
                    e.Range.StartColumn
                    e.Range.EndLine
                    e.Range.EndColumn
                    e.Message)
            >> String.concat System.Environment.NewLine

    type CompileDynamicAssemblySideEffect =
        | CompileDynamicAssemblySideEffect of code: string
        interface ISideEffect<Result<DynamicAssembly, ErrorInfo list>>

    let compile sourceCode =
        Effect.Of(CompileDynamicAssemblySideEffect sourceCode)
        |> Effect.wrap

module DynamicAssemblyCache =
    type GetDynamicAssemblySideEffect() =
        interface ISideEffect<DynamicAssembly>

    type SetDynamicAssemblySideEffect =
        | SetDynamicAssemblySideEffect of assembly: DynamicAssembly
        interface ISideEffect<unit>

    let get =
        Effect.Of(GetDynamicAssemblySideEffect())
        |> Effect.wrap

    let set assembly =
        Effect.Of(SetDynamicAssemblySideEffect assembly)
        |> Effect.wrap
