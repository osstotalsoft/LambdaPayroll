namespace LambdaPayroll.Infra

open LambdaPayroll.Application.InfraEffects

module CodeGenerationService =
    open System
    open LambdaPayroll.Domain
    open CodeGenerationService
    open NBB.Core.FSharp.Data
    open NBB.Core.FSharp.Data.State

   
    let private elemExpression ({ Code = ElemCode (code); Type = typ; DataType = dataType }: ElemDefinition) =
        match typ with
        | Db _ -> sprintf "HrAdmin.readFromDb<%s> \"%s\"" dataType.Name code
        | Formula { Formula = formula } -> sprintf "%s" (formula |> FormulaParser.stripDepMarkers)

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

    let generateSourceCode (GenerateSourceCodeSideEffect store) =
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

    let generateExpression (GenerateExpressionSideEffect formula) =
        Ok <| FormulaParser.stripDepMarkers formula


module GeneratedCodeCache =
    let mutable private cache: string option = None

    let get (_: GeneratedCodeCache.GetGeneratedCodeSideEffect)  =
        match cache with
        | Some(sourceCode) -> Ok sourceCode
        | None -> Error "Generated source code not available"

    let set (GeneratedCodeCache.SetGeneratedCodeSideEffect sourceCode) =
        cache <- Some sourceCode

    