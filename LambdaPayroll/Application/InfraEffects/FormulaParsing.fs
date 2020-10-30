namespace LambdaPayroll.Application.InfraEffects

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open ElemAlgebra
open LambdaPayroll.Domain

module FormulaParsingService =
    type GetFormulaDepsSideEffect =
        | GetFormulaDepsSideEffect of formula: string * codes: Set<ElemCode>
        interface ISideEffect<string list>

    let getFormulaDeps formula codes =
        Effect.Of(GetFormulaDepsSideEffect(formula, codes))
        |> Effect.wrap
