namespace LambdaPayroll.Application.Evaluation

open NBB.Core.Abstractions
open NBB.Core.Effects.FSharp
open Core
open LambdaPayroll.Domain

open LambdaPayroll.Application.InfraEffects
open System


module EvaluateSingleCode =
    type Query =
        { ElemCode: string
          ContractId: int
          Year: int
          Month: int }
        interface IQuery<Result<obj, string>> with
            member _.GetResponseType(): Type = typeof<Result<obj, string>>

    let handle (query: Query) =
        let code = ElemCode query.ElemCode

        let ctx =
            ContractId query.ContractId, YearMonth(query.Year, query.Month)

        effect {
            let! (DynamicAssembly elemAssembly) = DynamicAssemblyCache.get
            let! result = ElemEvaluationService.evaluateElem (ElemStore elemAssembly) code ctx

            return Some result
        }

module EvaluateMultipleCodes =
    type Query =
        { ElemCodes: string list
          ContractId: int
          Year: int
          Month: int }

        interface IQuery<Result<obj list, string>> with
            member _.GetResponseType(): Type = typeof<Result<obj list, string>>

    let handle (query: Query) =
        let codes = query.ElemCodes |> List.map ElemCode

        let ctx =
            ContractId query.ContractId, YearMonth(query.Year, query.Month)

        effect {
            let! (DynamicAssembly elemAssembly) = DynamicAssemblyCache.get
            let! result = ElemEvaluationService.evaluateElems (ElemStore elemAssembly) codes ctx

            return Some result
        }

module EvaluateExpression = 
    open LambdaPayroll.Application.InfraEffects.CodeGenerationService
    type Query =
        { Expression: string
          ContractId: int
          Year: int
          Month: int }
        
        interface IQuery<Result<obj, string>> with
            member _.GetResponseType(): Type = typeof<Result<obj, string>>
    
    let handle (query: Query) =
        let ctx = ContractId query.ContractId, YearMonth(query.Year, query.Month)

        effect {
            match! (generatExpression query.Expression) with
            | Ok expression -> 
                let! session = InteractiveEvalSessionCache.get
                let! result = ElemEvaluationService.evaluateExpression session expression ctx

                return Some result
            | Error e -> 
                return Some <| Error e
        }