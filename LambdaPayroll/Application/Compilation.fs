namespace LambdaPayroll.Application.Compilation

open System
open NBB.Core.Effects.FSharp
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

                    let! assembly = DynamicAssemblyService.compile sourceCode
                    do! DynamicAssemblyCache.set assembly
                | Error e -> 
                    do! Exception.throw e
           }

module GetGeneratedCode =
    type Query = unit

    let handler (_: Query) =
        effect {
            let! generatedCode = GeneratedCodeCache.get

            return generatedCode
        }