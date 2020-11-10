namespace LambdaPayroll.Infra

open LambdaPayroll.Application.InfraEffects

module CodeGenerationService =
    open System
    open LambdaPayroll.Domain
    open CodeGenerationService
    open NBB.Core.FSharp.Data
    open NBB.Core.FSharp.Data.State

    let private recordDefinition (ElemCode code) (elemDef: DbCollectionElemDefinition) =
        let recordTypeName = sprintf "%sRecord" code
        let recordType = 
            sprintf "type %s = \n    { %s }"
                recordTypeName
                (elemDef.Columns |> List.map (fun colDef -> (sprintf "%s: %s" colDef.ColumnName colDef.ColumnDataType)) |> String.concat ";\n      ")
        let extensions =
            elemDef.Columns 
            |> List.map (fun colDef -> 
                sprintf 
                    "[<Extension>]\n    static member %s (a: PayrollElem<%s>) = PayrollElem.map (fun x -> x.%s) a" 
                    colDef.ColumnName recordTypeName colDef.ColumnName)
            |> String.concat "\n    "
        
        (sprintf "%s

[<Extension>]
type %sExtensions =
    %s
" recordType code extensions)

    let private elemExpression ({ Code = ElemCode (code); Type = typ; DataType = dataType }: ElemDefinition) =
        match typ with
        | DbScalar {TableName=tableName; ColumnName=columnName} -> 
            sprintf 
                "HrAdmin.readScalarFromDb<%s> (ElemCode \"%s\") { TableName = \"%s\"; ColumnName = \"%s\" }" 
                dataType.Name code tableName columnName
        | Formula { Formula = formula } -> sprintf "%s" (formula |> FormulaParser.stripDepMarkers)
        | DbCollection {TableName = tableName; Columns= columnDefs} ->
            let recordTypeName = sprintf "%sRecord" code
            let columns =
                sprintf "[%s]"
                    (columnDefs |> List.map (fun colDef -> (sprintf "{ColumnName= \"%s\"; ColumnDataType = \"%s\"}" colDef.ColumnName colDef.ColumnDataType)) |> String.concat "; ")

            (sprintf "
HrAdmin.readCollectionFromDb<%s>
        (ElemCode \"%s\") { 
            TableName = \"%s\"
            Columns = %s}" recordTypeName code tableName columns)
            

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

open ElemAlgebra
open Combinators
open DefaultPayrollElems
open LambdaPayroll.Domain
open NBB.Core.Effects.FSharp
open System
open System.Runtime.CompilerServices  

"

    let generateSourceCode (GenerateSourceCodeSideEffect store) =
        let rec buildLinesMultipleElems (allCodes: Set<ElemCode>) (elems: ElemCode list)  =
            state {
                let! results = elems |> List.traverseState (buildLinesSingleElem allCodes)
                return results |> List.sequenceResult |> Result.map List.concat
            }
        and buildLinesSingleElem (allCodes: Set<ElemCode>) (elem: ElemCode): State<Set<ElemCode>, Result<string list, string>> =
            let buildLines elemDefinition=
                let crtLine = elemstatement elemDefinition
                state {
                    match elemDefinition with
                    | { Type = Formula { Formula = formula } } ->
                        let deps = formula |> FormulaParser.getDeps allCodes |> List.map ElemCode
                        let! depsLines = deps |> buildLinesMultipleElems allCodes

                        return depsLines |> Result.map (append crtLine)
                    | { Type = DbCollection colElemDef} ->
                        let recordDefinition = recordDefinition elem colElemDef
                        return Ok([recordDefinition; crtLine])
                    | _ -> return Ok([crtLine])
                }
            state {
                let! processedElems = State.get ()
                if processedElems.Contains(elem) then
                    return Ok(List.empty)
                else
                    let! elemResult = 
                        ElemDefinitionStore.findElemDefinition store elem
                        |> Result.traverseState buildLines
                        
                    do! State.modify(fun processedElems -> processedElems.Add elem)
                    return elemResult |> Result.join
            }
          
        let buildProgramLines  = 
            state {
                let allCodes = ElemDefinitionStore.getAllCodes store
                let! result = allCodes |> buildLinesMultipleElems (allCodes |> Set)
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

    