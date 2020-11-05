﻿namespace LambdaPayroll.Infra

open LambdaPayroll.Application.InfraEffects
open LambdaPayroll.Domain

module FormulaParser =
    open System.Text.RegularExpressions
    let private pattern =  @"@([a-zA-Z0-9_]+)"
    let private evaluator (m : Match) = m.Groups.[1].Value

    let getDeps (defs: Set<ElemCode>) (formulaWithTokens: string) =
        Regex.Matches(formulaWithTokens, "([a-zA-Z0-9_]+)", RegexOptions.Compiled)
        |> Seq.cast<Match>
        |> Seq.map evaluator
        |> Seq.where (ElemCode >> defs.Contains)
        |> Set
        |> Set.toList

    let stripDepMarkers (formulaWithTokens) =
        Regex.Replace(formulaWithTokens, pattern, MatchEvaluator evaluator, RegexOptions.Compiled)

module FormulaParsingService = 
    open FormulaParsingService

    let getFormulaDeps (GetFormulaDepsSideEffect(formula, allCodes)) =
        FormulaParser.getDeps allCodes formula