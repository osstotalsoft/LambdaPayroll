namespace LambdaPayroll.Domain

open System
open NBB.Core.Effects.FSharp
open NBB.Core.FSharp.Data.Reader
open NBB.Core.Effects.FSharp.Data.ReaderEffect
open NBB.Core.Effects.FSharp.Data.ReaderStateEffect
open NBB.Core.Effects.FSharp.Data.StateEffect
open NBB.Core.FSharp.Data
open Core

module ElemEvaluationService =
    type EvaluateElem = DynamicAssembly -> ElemCode -> PayrollElemContext -> Effect<Result<obj, string>>

    let evaluateElem: EvaluateElem =
        fun dynamicAssembly elemCode elemContext ->
            DynamicAssembly.findPayrollElem dynamicAssembly elemCode
            |> Result.traverseEffect(fun payrollElem -> payrollElem elemContext) 
            |> Effect.map Result.join

    //let evaluateElem2: EvaluateElem =
    //    fun assembly elemCode elemContext ->
    //        effect {
    //            let payrollElemResult =
    //                DynamicAssembly.findPayrollElem assembly elemCode

    //            match payrollElemResult with
    //            | Ok payrollElem ->
    //                let! result = payrollElem elemContext
    //                return result
    //            | Error e -> return Error e
    //        }

    type EvaluateElems = DynamicAssembly -> ElemCode list -> PayrollElemContext -> Effect<Result<obj list, string>>

    let evaluateElems: EvaluateElems =
        fun dynamicAssembly elemCodes elemContext ->
            elemCodes
            |> List.traverseEffect (fun elemCode -> evaluateElem dynamicAssembly elemCode elemContext)
            |> Effect.map List.sequenceResult

//type EvaluateElemsMultipleContexts = ElemDefinitionStore -> ElemCode list -> ComputationCtx list -> Effect<Result<obj list list, string>>
//let evaluateElemsMultipleContexts : EvaluateElemsMultipleContexts =
//    fun elemDefinitionStore elemCodes ctxs ->
//        effect {
//            let x = evaluateElems elemDefinitionStore elemCodes
//            let! results = ctxs |> List.traverseEffect (ReaderEffect.run x)
//            let result = results |> List.sequenceResult
//            return result
//        }
