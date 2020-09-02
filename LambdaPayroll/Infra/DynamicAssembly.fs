namespace LambdaPayroll.Infra

open System
open FSharp.Data
open LambdaPayroll.Domain

module DynamicAssembly =

    module DynamicAssemblyCache =
        let mutable private cache: DynamicAssembly option = None

        let get (_: DynamicAssemblyCache.GetDynamicAssemblySideEffect)  =
            match cache with
            | Some(assembly) -> assembly
            | None ->
                failwith "Assembly not available"

        let set (DynamicAssemblyCache.SetDynamicAssemblySideEffect assembly) =
            cache <- Some assembly

    module DynamicAssembly =
        open Core
        open System.IO
        open FSharp.Compiler.SourceCodeServices
        open DynamicAssemblyService

        let compile (CompileDynamicAssemblySideEffect sourceCode) : Result<DynamicAssembly, ErrorInfo list> =
            let fn = Path.GetTempFileName()
            let fn2 = Path.ChangeExtension(fn, ".fs")
            File.WriteAllText(fn2, sourceCode)
            let checker = FSharpChecker.Create()

            let errors, exitCode, dynAssembly =
                checker.CompileToDynamicAssembly
                    ([| "-o"
                        "Generated.dll"
                        "-a"
                        fn2
                        "-r"
                        "LambdaPayroll.dll"
                        "-r"
                        "FSharp.Data.SqlClient"
                        "-r"
                        "NBB.Core.Effects.FSharp" |],
                     execute = None)
                |> Async.RunSynchronously

            if exitCode <> 0 then
                errors 
                    |> Seq.map (fun e -> {Message = e.Message; Severity = e.Severity.ToString(); Range = {StartLine = e.Start.Line; StartColumn = e.Start.Column; EndLine = e.End.Line; EndColumn = e.End.Column};}) 
                    |> Seq.toList
                    |> Error
            else
                let assembly = dynAssembly.Value
                Ok (DynamicAssembly assembly)
