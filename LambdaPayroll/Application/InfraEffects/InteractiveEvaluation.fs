namespace LambdaPayroll.Application.InfraEffects

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open System.Reflection
open Core
open FSharp.Compiler.Interactive.Shell
open LambdaPayroll.Domain

//type InteractiveEvaluationSession = InteractiveEvaluationSession of FsiEvaluationSession

module InteractiveEvalSessionService =
    type Range = {StartLine: int; EndLine: int; StartColumn: int; EndColumn: int}
    type ErrorInfo = {Message: string; Severity: string; Range: Range option}

    module ErrorInfo =
        let format : (ErrorInfo list -> string) = 
            Seq.map (fun e -> 
                match e.Range with 
                | Some range -> sprintf "%s: (%i,%i-%i,%i) %s" e.Severity range.StartLine range.StartColumn range.EndLine range.EndColumn e.Message
                | None -> sprintf "%s:  %s" e.Severity e.Message)
            >> String.concat System.Environment.NewLine

    type CreateInteractiveEvalSessionSideEffect =
        | CreateInteractiveEvalSessionSideEffect of sourceCode: string
        interface ISideEffect<Result<InteractiveEvaluationSession, ErrorInfo list>>

    let create sourceCode =
        Effect.Of(CreateInteractiveEvalSessionSideEffect sourceCode)
        |> Effect.wrap

    type EvalInteractionSideEffect =
        | EvalInteractionSideEffect of session: InteractiveEvaluationSession * expression: string
        interface ISideEffect<Result<obj, ErrorInfo list>>

    let evalInteraction sideEff = Effect.Of (EvalInteractionSideEffect sideEff) |> Effect.wrap

module InteractiveEvalSessionCache =
    type GetInteractiveEvalSessionSideEffect() =
        interface ISideEffect<InteractiveEvaluationSession>

    type SetInteractiveEvalSessionSideEffect =
        | SetInteractiveEvalSessionSideEffect of session: InteractiveEvaluationSession
        interface ISideEffect<unit>

    let get =
        Effect.Of(GetInteractiveEvalSessionSideEffect())
        |> Effect.wrap

    let set session =
        Effect.Of(SetInteractiveEvalSessionSideEffect session)
        |> Effect.wrap
