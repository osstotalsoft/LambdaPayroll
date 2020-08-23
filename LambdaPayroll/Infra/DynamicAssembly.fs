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

        let compile (DynamicAssemblyService.CompileDynamicAssemblySideEffect sourceCode) : DynamicAssembly =
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
                failwith (errors |> Seq.map (fun e -> e.Message) |> String.concat Environment.NewLine)
            else
                let assembly = dynAssembly.Value
                DynamicAssembly assembly
