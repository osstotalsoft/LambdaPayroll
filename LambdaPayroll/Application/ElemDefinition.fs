namespace LambdaPayroll.Application

open NBB.Core.Effects.FSharp
open LambdaPayroll.PublishedLanguage
open NBB.Application.DataContracts
open LambdaPayroll.Domain
open NBB.Core.Evented.FSharp
open System

module AddDbElemDefinition =
    let handler (command: AddDbElemDefinition) =
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
        }

module AddFormulaElemDefinition =
    let handler (command: AddFormulaElemDefinition) =
        effect {
            let! store = ElemDefinitionStoreRepo.loadCurrent
            let! eventedStore = 
                ElemDefinitionStore.addFormulaElem
                    (command.ElemCode|> ElemCode) 
                    {Formula = command.Formula; Deps = [] }
                    (command.DataType |> Type.GetType) 
                    store
            do! eventedStore |> Evented.run |> ElemDefinitionStoreRepo.save 
            do! eventedStore |> Evented.exec |> Mediator.dispatchEvents

            let event: ElemDefinitionAdded = {ElemCode=command.ElemCode; Metadata = EventMetadata.Default()}
            do! MessageBus.publish event
        }  
