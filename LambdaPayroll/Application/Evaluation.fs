namespace LambdaPayroll.Application.Evaluation

open NBB.Core.Effects.FSharp
open Core
open LambdaPayroll.Domain

module EvaluateSingleCode =
    type Query =
        { ElemCode: string
          ContractId: int
          Year: int
          Month: int }

    let handler (query: Query) =
        let code = ElemCode query.ElemCode
        let ctx = ContractId query.ContractId, YearMonth(query.Year, query.Month)

        effect {
            let! elemAssembly = DynamicAssemblyCache.get
            let! result = ElemEvaluationService.evaluateElem elemAssembly code ctx

            return result
        }

module EvaluateMultipleCodes =
    type Query =
        { ElemCodes: string list
          ContractId: int
          Year: int
          Month: int }

    let handler (query: Query) =
        let codes = query.ElemCodes |> List.map ElemCode
        let ctx = ContractId query.ContractId, YearMonth(query.Year, query.Month)

        effect {
            let! elemAssembly = DynamicAssemblyCache.get
            let! result = ElemEvaluationService.evaluateElems elemAssembly codes ctx

            return result
        }
