namespace LambdaPayroll.Application.Compilation

open System
open NBB.Core.Effects.FSharp
open NBB.Core.Abstractions
open Core
open LambdaPayroll.Domain

module Compile =
    type Command = unit

    let handler (_: Command) =
           effect {
                let! store = ElemDefinitionStoreRepo.loadCurrent
                match codeGenerationService.generateSourceCode store with
                | Ok sourceCode -> 
                    do! GeneratedCodeCache.set sourceCode

                    match! DynamicAssemblyService.compile sourceCode with
                    | Ok assembly ->
                        do! DynamicAssemblyCache.set assembly
                    | Error errors ->
                        do! Exception.throw (errors |> Seq.map (fun e -> e.Message) |> String.concat Environment.NewLine)
                | Error e -> 
                    do! Exception.throw e
           }

module GetGeneratedCode =
    
    type Query () =
        interface IQuery<Result<string, string>> with
            member _.GetResponseType(): Type = typeof<Result<string, string>>

    let handle (_: Query) =
        effect {
            let! generatedCode = GeneratedCodeCache.get

            return Some generatedCode
        }