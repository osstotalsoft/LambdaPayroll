namespace LambdaPayroll.Application.InfraEffects

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open ElemAlgebra
open LambdaPayroll.Domain

module FormulaParsingService =
    type GetFormulaDepsSideEffect =
        | GetFormulaDepsSideEffect of formula: string * allCodes: Set<ElemCode>
        interface ISideEffect<string list>

    let getFormulaDeps formula allcodes =
        Effect.Of(GetFormulaDepsSideEffect(formula, allcodes))
        |> Effect.wrap
