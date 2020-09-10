namespace LambdaPayroll.Application.InfraEffects

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open Core

module FormulaParsingService =
    type GetFormulaDepsSideEffect =
        | GetFormulaDepsSideEffect of formula: string
        interface ISideEffect<string list>

    let getFormulaDeps formula =
        Effect.Of(GetFormulaDepsSideEffect formula)
        |> Effect.wrap
