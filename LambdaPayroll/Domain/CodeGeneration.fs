namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open Core

module codeGenerationService =
    open System
    open NBB.Core.FSharp.Data

    module FormulaParser =
        open System.Text.RegularExpressions
        let private pattern =  @"@([^\s]+)"
        let private evaluator (m : Match) = m.Groups.[1].Value

        let getDeps (formulaWithTokens) =
            Regex.Matches(formulaWithTokens, pattern, RegexOptions.Compiled)
            |> Seq.cast<Match>
            |> Seq.map evaluator
            |> Seq.toList

        let getText (formulaWithTokens) =
            Regex.Replace(formulaWithTokens, pattern, MatchEvaluator evaluator, RegexOptions.Compiled)

    let private elemExpression ({ Code = ElemCode (code); Type = typ; DataType = dataType }: ElemDefinition) =
        match typ with
        | Db _ -> sprintf "HrAdmin.readFromDb<%s> \"%s\"" dataType.Name code
        | Formula { Formula = formula } -> sprintf "%s" (formula |> FormulaParser.getText)

    let private elemstatement (elemDefinition: ElemDefinition) =
        let { Code = ElemCode (code) } = elemDefinition
        sprintf "let %s = %s" code (elemExpression elemDefinition)

    let private concat = String.concat Environment.NewLine

    let private header = "
module Generated

open Core
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp
"

    let generateSourceCode (store: ElemDefinitionStore) =
        let rec eval (defs: Set<ElemCode>) (elem: ElemCode): Result<Set<ElemCode> * string, string> =
            ElemDefinitionStore.findElemDefinition store elem
            |> Result.bind (fun elemDefinition ->

                if defs.Contains(elem) then
                    Ok(defs, String.Empty)
                else
                    let crtLine = elemstatement elemDefinition
                    match elemDefinition with
                    | { Type = Formula { Formula = formula } } ->
                        formula 
                        |> FormulaParser.getDeps
                        |> List.map (ElemCode)
                        |> List.fold folder (Ok(defs, String.Empty))
                        |> Result.map (fun (defs1, lines1) -> (defs1.Add elem, [ lines1; crtLine ] |> concat))
                    | _ -> Ok(defs.Add elem, crtLine))

        and folder =
            fun (acc: Result<Set<ElemCode> * string, string>) code ->
                acc
                |> Result.bind (fun (defs, lines) ->
                    eval defs code
                    |> Result.map (fun (defs1, lines1) -> (Set.union defs defs1, [ lines; lines1 ] |> concat)))


        ElemDefinitionStore.getAllCodes store
        |> List.fold folder (Ok(Set.empty, header))
        |> Result.map snd

module GeneratedCodeCache =
    type GetGeneratedCodeSideEffect() =
        interface ISideEffect<Result<string, string>>

    type SetGeneratedCodeSideEffect =
        | SetGeneratedCodeSideEffect of sourceCode: string
        interface ISideEffect<unit>

    let get =
        Effect.Of(GetGeneratedCodeSideEffect())
        |> Effect.wrap

    let set sourceCode =
        Effect.Of(SetGeneratedCodeSideEffect sourceCode)
        |> Effect.wrap