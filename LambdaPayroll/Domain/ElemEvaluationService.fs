namespace LambdaPayroll.Domain

open NBB.Core.Effects.FSharp
open NBB.Core.FSharp.Data.Reader
open NBB.Core.Effects.FSharp.Data.ReaderEffect
open NBB.Core.Effects.FSharp.Data.ReaderStateEffect
open NBB.Core.Effects.FSharp.Data.StateEffect
open FSharp.Compiler.Interactive.Shell
open NBB.Core.FSharp.Data
open ElemAlgebra

type ElemStore = ElemStore of System.Reflection.Assembly
module ElemRepo =
    open NBB.Core.Effects
    
    type FindPayrollElemSideEffect =
        | FindPayrollElemSideEffect of store:ElemStore * elemCode: ElemCode
        interface ISideEffect<Result<PayrollElem<obj>, string>>
     
    let findPayrollElem elemCode = Effect.Of (FindPayrollElemSideEffect elemCode)

type InteractiveEvaluationSession =  InteractiveEvaluationSession of FsiEvaluationSession

module InteractivePayrollElemService =
    open NBB.Core.Effects
    type EvalToPayrollElemSideEffect =
        | EvalToPayrollElemSideEffect of session: InteractiveEvaluationSession * expression: string
        interface ISideEffect<Result<PayrollElem<obj>, string>>

    let evalToPayrollElem expression = Effect.Of (EvalToPayrollElemSideEffect expression)

module ElemEvaluationService =
    type EvaluateElem = ElemStore -> ElemCode -> PayrollElemContext -> Effect<Result<obj, string>>

    let evaluateElem: EvaluateElem =
       fun elemStore elemCode elemContext ->
           effect {
               let! payrollElemResult = ElemRepo.findPayrollElem (elemStore, elemCode)

               match payrollElemResult with
               | Ok payrollElem -> return! run payrollElem elemContext
               | Error e -> return Error e
           }

    type EvaluateElems = ElemStore -> ElemCode list -> PayrollElemContext -> Effect<Result<obj list, string>>

    let evaluateElems: EvaluateElems =
        fun elemStore elemCodes elemContext ->
            elemCodes
            |> List.traverseEffect (fun elemCode -> evaluateElem elemStore elemCode elemContext)
            |> Effect.map List.sequenceResult

    open InteractivePayrollElemService
    type EvaluateExpression = InteractiveEvaluationSession -> string -> PayrollElemContext -> Effect<Result<obj, string>>
    
    let evaluateExpression : EvaluateExpression =
        fun interactiveEvalSession expression elemContext ->
            effect {
                let! result = evalToPayrollElem (interactiveEvalSession, expression)
                return!
                    result 
                    |> Result.traverseEffect (fun payrollElem -> run payrollElem elemContext)
                    |> Effect.map Result.join
            }

