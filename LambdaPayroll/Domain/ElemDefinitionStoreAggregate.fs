namespace LambdaPayroll.Domain

open System
open NBB.Core.Abstractions
open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open NBB.Core.Evented.FSharp

[<CustomEquality; NoComparison>]
type ElemDefinitionStore = {
    Id: ElemDefinitionStoreId
    ElemDefinitions: Map<ElemCode, ElemDefinition>
}
with
    override this.Equals(obj) =
        match obj with
        | :? ElemDefinitionStore as eds -> this.Id = eds.Id
        | _ -> false
    override this.GetHashCode() =
        hash this.Id
and ElemDefinitionStoreId = ElemDefinitionStoreId of Guid
and ElemCode = ElemCode of string
and 
    [<CustomEquality; NoComparison>]
    ElemDefinition = {
        Code: ElemCode
        Type: ElemType
        DataType: Type
    }
    with
    override this.Equals(obj) =
        match obj with
        | :? ElemDefinition as ed -> this.Code = ed.Code
        | _ -> false
    override this.GetHashCode() =
        hash this.Code
and ElemType = 
    | DbScalar of DbScalarElemDefinition
    | DbCollection of DbCollectionElemDefinition
    | Formula of FormulaElemDefinition
and DbScalarElemDefinition = { TableName: string; ColumnName: string }
and DbCollectionElemDefinition = { TableName: string; Columns: DbColumnDefinition list }
and FormulaElemDefinition = { Formula: string; Deps: string list }
and DbColumnDefinition =  { ColumnName: string; ColumnDataType: string}

type ElemDefinitionStoreEvent = 
    | ElemDefinitionStoreCreated of ElemDefinitionStore
    | ElemDefinitionAdded of elemDefinitionStoreId: ElemDefinitionStoreId * elemDefinition: ElemDefinition
    interface IEvent
        with member __.EventId: Guid = Guid.Empty //TODO: TBD if EventId should be stored on every element

module ElemDefinitionStore =
    let create (elemDefs: ElemDefinition seq) = 
        let elemDefinitions = elemDefs |> Seq.map (fun elemDef -> (elemDef.Code, elemDef)) |> Map.ofSeq
        in {Id = Guid.Empty |> ElemDefinitionStoreId; ElemDefinitions = elemDefinitions}

    let createNew (elemDefs: ElemDefinition seq) = 
        evented {
            let store = create elemDefs
            do! addEvent ElemDefinitionStoreCreated 
            return store
        }

    let addDbElem (code:ElemCode) (dbElemDefinition: DbScalarElemDefinition) (dataType: Type) (store: ElemDefinitionStore) =
        effect {
            if store.ElemDefinitions.ContainsKey code 
            then do! Exception.throw "Elem already defined"
            return evented {
                let elemDef = {
                    Code = code
                    Type = DbScalar(dbElemDefinition)
                    DataType = dataType
                }
                do! addEvent (ElemDefinitionAdded (store.Id, elemDef))
                return {store with ElemDefinitions = store.ElemDefinitions.Add (code, elemDef)}   
            }
        }

    let addFormulaElem (code:ElemCode) (formulaElemDefinition: FormulaElemDefinition) (dataType: Type) (store: ElemDefinitionStore) =
        effect {
            if store.ElemDefinitions.ContainsKey code 
            then do! Exception.throw "Elem already defined"            
            return evented {
                let elemDef = {
                    Code = code
                    Type = Formula(formulaElemDefinition)
                    DataType = dataType
                }
                do! addEvent (ElemDefinitionAdded (store.Id, elemDef))
                return {store with ElemDefinitions = store.ElemDefinitions.Add (code, elemDef)}
            }
        }

    let findElemDefinition ({ElemDefinitions=elemDefinitions}) elemCode = 
        match (elemDefinitions.TryFind elemCode) with
            | None -> sprintf "could not find definition %A" elemCode |> Result.Error
            | Some elemDefinition -> Result.Ok elemDefinition

    let getAllCodes (store: ElemDefinitionStore) =
        store.ElemDefinitions 
        |> Map.toList
        |> List.map fst


module ElemDefinitionStoreRepo =
    type LoadCurrentElemDefinitionStoreSideEffect () =
        interface ISideEffect<ElemDefinitionStore>
    type SaveElemDefinitionStoreSideEffect = SaveElemDefinitionStoreSideEffect of store: ElemDefinitionStore * events: ElemDefinitionStoreEvent list
        with interface ISideEffect<unit>

    let loadCurrent = Effect.Of (LoadCurrentElemDefinitionStoreSideEffect ())
    let save store = Effect.Of (SaveElemDefinitionStoreSideEffect store)

