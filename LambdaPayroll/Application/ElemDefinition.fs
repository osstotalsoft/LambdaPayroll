namespace LambdaPayroll.Application

open NBB.Core.Effects.FSharp
open LambdaPayroll.PublishedLanguage
open NBB.Application.DataContracts
open LambdaPayroll.Domain
open LambdaPayroll.Application.InfraEffects
open NBB.Core.Evented.FSharp
open System

module AddDbElemDefinition =
    let handle (command: AddDbElemDefinition) =
        effect {
            let! store = ElemDefinitionStoreRepo.loadCurrent
            let! eventedStore = 
                ElemDefinitionStore.addDbElem 
                    (command.ElemCode|> ElemCode) 
                    {TableName = command.Table; ColumnName = command.Column} 
                    (command.DataType |> Type.GetType) 
                    store
            do! eventedStore |> Evented.run |> ElemDefinitionStoreRepo.save 
            do! eventedStore |> Evented.exec |> Mediator.dispatchEvents

            let event: ElemDefinitionAdded = {ElemCode=command.ElemCode; Metadata = EventMetadata.Default()}
            do! MessageBus.publish event
            return Some ()
        }

module AddFormulaElemDefinition =
    let validate (command: AddFormulaElemDefinition) = 
        effect {
            let elemDef = {
                Code = command.ElemCode |> ElemCode
                Type = {Formula = command.Formula; Deps = [] } |> Formula
                DataType = (command.DataType |> Type.GetType) 
            }
            let! store = ElemDefinitionStoreRepo.loadCurrent
            let store' = {store with ElemDefinitions = store.ElemDefinitions.Add (elemDef.Code, elemDef)}

            match! CodeGenerationService.generateSourceCode (store') with
            | Ok sourceCode -> 
                match! DynamicAssemblyService.compile sourceCode with
                | Ok _ ->
                    return None
                | Error errors ->
                    let error = errors |> DynamicAssemblyService.ErrorInfo.format
                    return Some <| failwith (sprintf "Compilation errors: \n%s" error)
            | Error error -> 
                return Some <| failwith error
        }

    let handle (command: AddFormulaElemDefinition) =
        effect {
            let! store = ElemDefinitionStoreRepo.loadCurrent
            let codes = store |> ElemDefinitionStore.getAllCodes |> Set
            let! formulaDeps = FormulaParsingService.getFormulaDeps command.Formula codes
            let! eventedStore = 
                           ElemDefinitionStore.addFormulaElem
                               (command.ElemCode|> ElemCode) 
                               {Formula = command.Formula; Deps = formulaDeps }
                               (command.DataType |> Type.GetType) 
                               store

            do! eventedStore |> Evented.run |> ElemDefinitionStoreRepo.save 
            do! eventedStore |> Evented.exec |> Mediator.dispatchEvents

            let event: ElemDefinitionAdded = {ElemCode=command.ElemCode; Metadata = EventMetadata.Default()}
            do! MessageBus.publish event

            return Some()
        }  

module ElemDefinitionAdded =
    open LambdaPayroll.Application.Compilation

    let handle (_: ElemDefinitionAdded) =
        effect {
            do! Mediator.sendCommand <| Compile.Command ()
            return Some()
        }