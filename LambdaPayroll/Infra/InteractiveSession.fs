namespace LambdaPayroll.Infra

open LambdaPayroll.Domain
open LambdaPayroll.Domain.InteractivePayrollElemService
open LambdaPayroll.Application.InfraEffects
open FSharp.Compiler.Interactive.Shell

module InteractiveEvalSession =
    module InteractiveEvalSessionCache =
        let mutable private cache: InteractiveEvaluationSession option = None

        let get (_: InteractiveEvalSessionCache.GetInteractiveEvalSessionSideEffect) =
            match cache with
            | Some session -> session
            | None -> failwith "Interactive evaluation session not available"

        let set (InteractiveEvalSessionCache.SetInteractiveEvalSessionSideEffect session) = 
            cache <- Some session

    module InteractiveSession =
        open Core
        open System.IO
        open System.Text
        open FSharp.Compiler.SourceCodeServices
        open InteractiveEvalSessionService


        let private header = "
#r \"LambdaPayroll.dll\"
#r \"System.Data.SqlClient\"
#r \"NBB.Core.Effects.FSharp\"

"
        let private mapFsiErrors  =
            Seq.map (fun (e: FSharpErrorInfo)   ->
                { Message = e.Message
                  Severity = e.Severity.ToString()
                  Range =
                      Some { 
                        StartLine = e.Start.Line
                        StartColumn = e.Start.Column
                        EndLine = e.End.Line
                        EndColumn = e.End.Column }})
            >> Seq.toList

        let private mapException (exn: exn) =
            [{Message =  exn.Message; Severity = "Error"; Range = None }]


        let createSession (CreateInteractiveEvalSessionSideEffect sourceCode): Result<InteractiveEvaluationSession, ErrorInfo list> =
            let sbOut = StringBuilder()
            let sbErr = StringBuilder()
            let  inStream = new StringReader("")
            let  outStream = new StringWriter(sbOut)
            let  errStream = new StringWriter(sbErr)

            // Build command line arguments & start FSI session
            let defaultArgs = [|"fsi.exe";"--noninteractive";"--nologo";"--gui-"|]

            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
            let fsiSession = FsiEvaluationSession.Create(fsiConfig, defaultArgs, inStream, outStream, errStream, collectible=true)

            let code = header + sourceCode.Replace("module Generated", "")

            let res, warnings = fsiSession.EvalInteractionNonThrowing code
            if (warnings.Length > 0) then
                warnings |> mapFsiErrors |> Error
            else 
                match res with
                | Choice1Of2 _ -> 
                    Ok <| InteractiveEvaluationSession fsiSession
                | Choice2Of2 (exn:exn) -> exn |> mapException |> Error

        // let evaluateInteraction (EvalInteractionSideEffect(InteractiveEvaluationSession session, expression)) =
        //     let res, warnings = session.EvalExpressionNonThrowing expression
        //     if (warnings.Length > 0) then
        //         warnings |> mapFsiErrors |> Error
        //     else 
        //         match res with
        //         | Choice1Of2 (Some value) -> Ok value.ReflectionValue
        //         | Choice1Of2 None ->  [{Message =  "Got no result"; Severity = "Error"; Range = None }] |> Error
        //         | Choice2Of2 (exn:exn) -> exn |> mapException |> Error

        
        let evalToPayrollElem (EvalToPayrollElemSideEffect (InteractiveEvaluationSession session, expression)) =
            let res, warnings = session.EvalExpressionNonThrowing expression
            if (warnings.Length > 0) then
                warnings |> mapFsiErrors  |> ErrorInfo.format |> Error
            else 
                match res with
                | Choice1Of2 (Some value) -> 
                    value.ReflectionValue |> DynamicAssembly.boxPayrollElemValue 
                | Choice1Of2 None ->   
                    [{Message =  "Got no result"; Severity = "Error"; Range = None }] |> ErrorInfo.format |> Error
                | Choice2Of2 (exn:exn) -> 
                    exn |> mapException |> ErrorInfo.format |> Error