namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open System.Reflection
open Core

type DynamicAssembly = DynamicAssembly of Assembly

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

    let findPayrollElem (DynamicAssembly assembly) (ElemCode elemCode): Result<PayrollElem<obj>, string> =
        assembly.GetTypes()
        |> Array.tryFind (fun t -> t.Name = "Generated")
        |> function
        | Some moduleType ->
            moduleType.GetProperties()
            |> Array.tryFind (fun f -> f.Name = elemCode)
            |> function
            | Some prop -> prop.GetValue(null) |> boxPayrollElemValue
            | None -> Error(sprintf "Property '%s' not found" elemCode)
        | None -> Error "Module `Generated` not found"

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
