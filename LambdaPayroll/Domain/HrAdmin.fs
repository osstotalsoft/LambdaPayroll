module HrAdmin

open Core
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain
open NBB.Core.Effects

type LoadSideEffect =
    { Definition: DbElemDefinition
      Context: PayrollElemContext }
    interface ISideEffect<Result<obj, string>>

let load definition (contractId, yearMonth) =
    (Effect.Of
        { Definition = definition
          Context = (contractId, yearMonth) })
    |> Effect.wrap


let readFromDb<'a> (code: string): PayrollElem<'a> =
    let cast (value: 'b) =
        if isNull value then Error "Value not found in HR DB"
        else if typeof<'a> = value.GetType() then Ok(box value :?> 'a)
        else Error "Invalid elem type"

    fun (contractId, yearMonth) ->
        effect {
            let! elemDefinitionStore = ElemDefinitionStoreRepo.loadCurrent

            match ElemDefinitionStore.findElemDefinition elemDefinitionStore (ElemCode code) with
            | Ok (elemDefinition) ->
                match elemDefinition.Type with
                | Db dbElemDefinition ->
                    let! result = load dbElemDefinition (contractId, yearMonth)
                    return result |> Result.bind cast
                | _ -> return Error "Invalid elem definition type"
            | Error e -> return Error e
        }

type GetOtherEmployeeContractsSideEffect =
    | GetOtherEmployeeContractsSideEffect of contractId: ContractId
    interface ISideEffect<ContractId list>

let getOtherEmployeeContracts contractId =
    (Effect.Of(GetOtherEmployeeContractsSideEffect contractId))
    |> Effect.wrap


type GetAllEmployeeContractsSideEffect =
    | GetAllEmployeeContractsSideEffect of contractId: ContractId
    interface ISideEffect<ContractId list>

let getAllEmployeeContracts contractId =
    (Effect.Of(GetAllEmployeeContractsSideEffect contractId))
    |> Effect.wrap
