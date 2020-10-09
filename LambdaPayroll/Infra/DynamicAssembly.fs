namespace LambdaPayroll.Infra

open ElemAlgebra
open LambdaPayroll.Domain
open LambdaPayroll.Application.InfraEffects
open System.Reflection

module DynamicAssembly =
    let boxPayrollElemValue (payrollElem: obj) =
        match payrollElem with
        | :? (PayrollElem<decimal>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<decimal list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<bool>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<bool list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<string>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<string list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<bigint>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<bigint list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<char>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<char list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<float>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<float list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<float32>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<float32 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<byte>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<byte list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<sbyte>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<sbyte list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int16>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int16 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint16>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint16 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint32>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint32 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int64>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<int64 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint64>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<uint64 list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<PayrollElemContext>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<PayrollElemContext list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<ContractId>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<ContractId list>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<YearMonth>) as elem -> elem |> PayrollElem.map box |> Ok
        | :? (PayrollElem<YearMonth list>) as elem -> elem |> PayrollElem.map box |> Ok
        | _ -> Error "Unsupported payrollElem type"



module DynamicAssemblyCache =
    open DynamicAssemblyCache

    let mutable private cache: DynamicAssembly option = None

    let get (_: GetDynamicAssemblySideEffect) =
        match cache with
        | Some (assembly) -> assembly
        | None -> failwith "Assembly not available"

    let set (SetDynamicAssemblySideEffect assembly) = cache <- Some assembly


module DynamicAssemblyService =
    open System.IO
    open FSharp.Compiler.SourceCodeServices
    open DynamicAssemblyService

    let compile (CompileDynamicAssemblySideEffect sourceCode): Result<DynamicAssembly, ErrorInfo list> =
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
                    "System.Data.SqlClient"
                    "-r"
                    "NBB.Core.Effects.FSharp" |],
                 execute = None)
            |> Async.RunSynchronously

        if exitCode <> 0 then
            errors
            |> Seq.map (fun e ->
                { Message = e.Message
                  Severity = e.Severity.ToString()
                  Range =
                      { StartLine = e.Start.Line
                        StartColumn = e.Start.Column
                        EndLine = e.End.Line
                        EndColumn = e.End.Column } })
            |> Seq.toList
            |> Error
        else
            let assembly = dynAssembly.Value
            Ok(DynamicAssembly assembly)

    open LambdaPayroll.Domain.ElemRepo
    let findPayrollElem (FindPayrollElemSideEffect ((ElemStore assembly), (ElemCode elemCode))): Result<PayrollElem<obj>, string> =
        assembly.GetTypes()
        |> Array.tryFind (fun t -> t.Name = "Generated")
        |> function
        | Some moduleType ->
            moduleType.GetProperties()
            |> Array.tryFind (fun f -> f.Name = elemCode)
            |> function
            | Some prop -> prop.GetValue(null) |> DynamicAssembly.boxPayrollElemValue
            | None -> Error(sprintf "Property '%s' not found" elemCode)
        | None -> Error "Module `Generated` not found"