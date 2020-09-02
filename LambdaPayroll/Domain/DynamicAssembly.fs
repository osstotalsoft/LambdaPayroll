namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open System.Reflection
open Core

type DynamicAssembly = DynamicAssembly of Assembly

module DynamicAssembly =
    let private cast (payrollElem: obj) =
        match payrollElem with 
        | :? PayrollElem<decimal> as elem   -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<int> as elem       -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<bool> as elem      -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<string> as elem    -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<bigint> as elem    -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<char> as elem      -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<float> as elem     -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<float32> as elem   -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<byte> as elem      -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<sbyte> as elem     -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<int16> as elem     -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<uint16> as elem    -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<uint32> as elem    -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<int64> as elem     -> elem |> PayrollElem.map box  |> Ok
        | :? PayrollElem<uint64> as elem    -> elem |> PayrollElem.map box  |> Ok
        | _ -> Error "Unsupported payrollElem type"

    let findPayrollElem (DynamicAssembly assembly) (ElemCode elemCode): Result<PayrollElem<obj>, string> =
        assembly.GetTypes()
        |> Array.tryFind (fun t -> t.Name = "Generated") 
        |> function
        | Some moduleType ->
            moduleType.GetProperties()
            |> Array.tryFind (fun f -> f.Name = elemCode)
            |> function
            | Some prop -> prop.GetValue(null) |> cast
            | None -> Error (sprintf "Property '%s' not found" elemCode)
        | None -> Error "Module `Generated` not found"

module DynamicAssemblyService =
    type Range = {StartLine: int; EndLine: int; StartColumn: int; EndColumn: int}
    type ErrorInfo = {Message: string; Severity: string; Range: Range}

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
