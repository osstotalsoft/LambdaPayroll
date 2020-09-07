namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open Core

module CodeGenerationService =
    open System
    open NBB.Core.FSharp.Data
    open NBB.Core.FSharp.Data.State

    module FormulaParser =
        open System.Text.RegularExpressions
        let private pattern =  @"@([a-zA-Z0-9_]+)"
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
        let expression = elemExpression elemDefinition
        let tab = "    "
        let indent (str: string) = 
            str.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (sprintf "%s%s" tab)
            |> String.concat Environment.NewLine

        if expression.Contains(Environment.NewLine) || (String.length expression) > 50 then
            sprintf "let %s = \n%s\n" code (indent expression)
        else
            sprintf "let %s = %s" code expression

    let private concat = String.concat Environment.NewLine
    let private append item list = list @ [item]
    let private prepend item list = [item] @ list

    let private header = "
module Generated

open Core
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp
"

    let generateSourceCode (store: ElemDefinitionStore) =
        let rec buildLinesMultipleElems(elems: ElemCode list) =
            state {
                let! results = elems |> List.traverseState buildLinesSingleElem
                return results |> List.sequenceResult |> Result.map List.concat
            }
        and buildLinesSingleElem (elem: ElemCode): State<Set<ElemCode>, Result<string list, string>> =
            let buildLines elemDefinition =
                let crtLine = elemstatement elemDefinition
                state {
                    match elemDefinition with
                    | { Type = Formula { Formula = formula } } ->
                        let deps = formula |> FormulaParser.getDeps |> List.map ElemCode
                        let! depsLines = deps |> buildLinesMultipleElems

                        return depsLines |> Result.map (append crtLine)
                    | _ -> return Ok([crtLine])
                }
            state {
                let! defs = State.get ()
                if defs.Contains(elem) then
                    return Ok(List.empty)
                else
                    let! elemResult = 
                        ElemDefinitionStore.findElemDefinition store elem
                        |> Result.traverseState buildLines
                        
                    do! State.modify(fun state -> state.Add elem)
                    return elemResult |> Result.join
            }
          
        let buildProgramLines  = 
            state {
                let! result = ElemDefinitionStore.getAllCodes store |> buildLinesMultipleElems
                return result |> Result.map (prepend header)
            }

        let lines, _ = State.run buildProgramLines Set.empty
        lines |> Result.map concat

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