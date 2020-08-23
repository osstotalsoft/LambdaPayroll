module UnitTests

open System
open Xunit
open LambdaPayroll.Domain


open FsUnit.Xunit
open NBB.Core.Effects.FSharp
open System.Threading.Tasks

module Handlers =
    ()
//    open LambdaPayroll.Domain.Parser
//    open LambdaPayroll.Domain.DbElemValue
//    open NBB.Core.Effects
//    open System.Threading

//    type DbResult = Result<obj, string>

//    type GenericSideEffectHandler(dbHandler : DbElemValue.LoadSideEffect -> DbResult, formulaHandler: ParseFormulaSideEffect -> ParseFormulaResult) =
//        interface ISideEffectHandler 
//        member _.Handle(sideEffect: obj, _ : CancellationToken) =
//           match (sideEffect) with
//                | :? LoadSideEffect as lse -> dbHandler(lse) :> obj 
//                | :? ParseFormulaSideEffect as pfe -> formulaHandler (pfe) :> obj 
//                | _ -> failwith "Unhandled side effect"

//    let getSideEffectMediator (dbHandler, formulaHandler)  =
//        { new ISideEffectMediator with
//            member this.Run<'TOutput> (sideEffect: ISideEffect<'TOutput>, cancellationToken) = 
//                match sideEffect with
//                | :? Thunk.SideEffect<'TOutput> as tse ->  tse.ImpureFn.Invoke(cancellationToken)
//                | _ -> GenericSideEffectHandler(dbHandler, formulaHandler).Handle(sideEffect, cancellationToken) :?> 'TOutput |> Task.FromResult
//        }

//[<Fact>]
//let ``It shoud evaluate data access element`` () =
    
//    // Arrange
//    let code1 = ElemCode "code1"
//    let loadElemDefinitions () =
//        seq { yield {Code = code1; Type = Db {TableName="aa"; ColumnName ="bb"}; DataType= typeof<int> }} 
//        |> ElemDefinitionStore.create
//        |> Effect.pure'

//    let ctx: ComputationCtx = ContractId (Guid.NewGuid()), YearMonth (2009, 1)

//    let factory = Handlers.getSideEffectMediator((fun _ -> Result.Ok (1:> obj)) , (fun _ -> {Func= (fun _ -> (1:>obj)); Parameters= []}))
//    let interpreter = NBB.Core.Effects.Interpreter(factory)

//    let eff = effect {
//          let! elemDefinitionStore = loadElemDefinitions ()
//          let! value = ElemEvaluationService.evaluateElem elemDefinitionStore code1 ctx

//          return value
//      }

//    // Act

//    let result = eff |> Effect.interpret interpreter |> Async.RunSynchronously

//    // Assert
//    let expected:Result<obj,string> = Ok (1:>obj)
//    result |> should equal expected



//[<Fact>]
//let ``It shoud evaluate formula without params`` () =
    
//    // Arrange
//    let code1 = ElemCode "code1"
//    let loadElemDefinitions () =
//        seq { yield {Code = code1; Type = Formula {Formula="1 + 2"; Deps =[]}; DataType= typeof<int> }} 
//        |> ElemDefinitionStore.create
//        |> Effect.pure'

//    let ctx: ComputationCtx = {PersonId = PersonId (Guid.NewGuid()); YearMonth = {Year = 2009; Month = 1}}

//    let factory = Handlers.getSideEffectMediator((fun _ -> Result.Ok (1:> obj)) , (fun _ -> {Func= (fun _ -> (3 :> obj)); Parameters= []}))
//    let interpreter = NBB.Core.Effects.Interpreter(factory)

//    let eff = effect {
//          let! elemDefinitionStore = loadElemDefinitions ()
//          let! value = ElemEvaluationService.evaluateElem elemDefinitionStore code1 ctx

//          return value
//    }

//    // Act

//    let result = eff |> Effect.interpret interpreter |> Async.RunSynchronously

//    // Assert
//    let expected:Result<obj,string> = Ok (3:>obj)
//    result |> should equal expected


//[<Fact>]
//let ``It shoud evaluate formula with params`` () =
    
//    // Arrange
//    let code1 = ElemCode "code1"
//    let code2 = ElemCode "code2"
//    let code3 = ElemCode "code3"

//    let loadElemDefinitions () =
//        seq { 
//            yield {Code = code1; Type = Formula {Formula="1m + code2 + code3"; Deps =[]} ;DataType= typeof<decimal>}
//            yield {Code = code2; Type = Db {TableName="aa"; ColumnName ="bb"}; DataType= typeof<decimal>}
//            yield {Code = code3; Type = Formula {Formula="1m + code2"; Deps =[]; }; DataType= typeof<decimal> }
//        } 
//        |> ElemDefinitionStore.create
//        |> Effect.pure'

//    let ctx: ComputationCtx = {PersonId = PersonId (Guid.NewGuid()); YearMonth = {Year = 2009; Month = 1}}

//    let formulaHandler ({Formula=formula} : Parser.ParseFormulaSideEffect) : Parser.ParseFormulaResult =
//        match formula with
//        | "1m + code2 + code3" -> {
//                Func = function 
//                        | ([|code2; code3|]) -> box(1m + (unbox<decimal> code2) +  (unbox<decimal> code3)) 
//                        | _ -> failwith "Invalid arguments"
//                Parameters=["code2"; "code3"]
//            }
//        | "1m + code2" -> {
//                Func= function
//                    | ([|code2|]) -> box(1m + (unbox<decimal> code2))
//                    | _ -> failwith "Invalid arguments"
//                Parameters=["code2"] 
//            }
//        | _ -> {Func= (fun _ -> (1:>obj)); Parameters= []}

//    let sideEffectMediator = Handlers.getSideEffectMediator((fun _ -> Result.Ok (4m:> obj)) , formulaHandler)
//    let interpreter = NBB.Core.Effects.Interpreter(sideEffectMediator)

//    let eff = effect {
//        let! elemDefinitionStore = loadElemDefinitions ()

//        let! result = ElemEvaluationService.evaluateElems elemDefinitionStore [code1; code2] ctx

//        return result
//    }

//    // Act
//    let result = eff |> Effect.interpret interpreter |> Async.RunSynchronously

//    // Assert
//    result |> should equal (Ok ([10m :> obj; 4m :> obj]) : Result<obj list, string>)