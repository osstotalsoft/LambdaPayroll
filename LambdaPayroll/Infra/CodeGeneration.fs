namespace LambdaPayroll.Infra

open LambdaPayroll.Application.InfraEffects

module CodeGenerationService =
    open System
    open LambdaPayroll.Domain
    open CodeGenerationService
    open NBB.Core.FSharp.Data
    open NBB.Core.FSharp.Data.State

    let private recordAccessorModule (ElemCode code) (elemDef: DbCollectionElemDefinition) =
        let accessors =
            elemDef.Columns 
            |> List.map (fun colDef -> (sprintf "let inline _%s a = PayrollElem.map (fun x -> (^a: (member %s: _) x)) a" colDef.ColumnName colDef.ColumnName))
            |> String.concat "\n    "
        
        (sprintf "[<AutoOpen>]
module %s =
    %s
" code accessors)
   
    let private elemExpression ({ Code = ElemCode (code); Type = typ; DataType = dataType }: ElemDefinition) =
        match typ with
        | DbScalar {TableName=tableName; ColumnName=columnName} -> 
            sprintf 
                "HrAdmin.readScalarFromDb<%s> (ElemCode \"%s\") { TableName = \"%s\"; ColumnName = \"%s\" }" 
                dataType.Name code tableName columnName
        | Formula { Formula = formula } -> sprintf "%s" (formula |> FormulaParser.stripDepMarkers)
        | DbCollection {TableName = tableName; Columns= columnDefs} ->
            
            let record = 
                sprintf "{|%s|}"
                    (columnDefs |> List.map (fun colDef -> (sprintf "%s: %s" colDef.ColumnName colDef.ColumnDataType)) |> String.concat "; ")

            let columns =
                sprintf "[%s]"
                    (columnDefs |> List.map (fun colDef -> (sprintf "{ColumnName= \"%s\"; ColumnDataType = \"%s\"}" colDef.ColumnName colDef.ColumnDataType)) |> String.concat "; ")

            (sprintf "
HrAdmin.readCollectionFromDb<%s>
        (ElemCode \"%s\") { 
            TableName = \"%s\"
            Columns = %s}" record code tableName columns)
            

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
open LambdaPayroll.Domain
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
                    | { Type = DbCollection colElemDef} ->
                        let recordAccessors = recordAccessorModule elem colElemDef
                        return Ok([crtLine; recordAccessors])
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

    