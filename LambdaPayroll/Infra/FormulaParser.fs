namespace LambdaPayroll.Infra

open LambdaPayroll.Application.InfraEffects

module FormulaParser =
    open System.Text.RegularExpressions
    let private pattern =  @"@([a-zA-Z0-9_]+)"
    let private evaluator (m : Match) = m.Groups.[1].Value

    let getDeps (formulaWithTokens) =
        Regex.Matches(formulaWithTokens, pattern, RegexOptions.Compiled)
        |> Seq.cast<Match>
        |> Seq.map evaluator
        |> Seq.toList

    let stripDepMarkers (formulaWithTokens) =
        Regex.Replace(formulaWithTokens, pattern, MatchEvaluator evaluator, RegexOptions.Compiled)

module FormulaParsingService = 
    open FormulaParsingService

    let getFormulaDeps (GetFormulaDepsSideEffect formula) =
        FormulaParser.getDeps formula