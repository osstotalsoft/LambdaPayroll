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


let readFromDb<'a> (ElemCode code) (dbElemDefinition: DbElemDefinition): PayrollElem<'a> =
    
    let cast (code: string) (value: 'b) =
        if isNull value then Error <| sprintf "Value for %s not found in HR DB" code
        else if typeof<'a> = value.GetType() then Ok(box value :?> 'a)
        else Error <| sprintf "Invalid elem type for %s (expected %s and received %s)" code (typeof<'a>).Name (value.GetType().Name)

    fun (contractId, yearMonth) ->
        effect {
            let! result = load dbElemDefinition (contractId, yearMonth)
            return result |> Result.bind (cast code)
        }

type GetOtherEmployeeContractsSideEffect =
    | GetOtherEmployeeContractsSideEffect of contractId: ContractId
    interface ISideEffect<ContractId list>

let getOtherEmployeeContracts contractId =
    (Effect.Of(GetOtherEmployeeContractsSideEffect contractId))
    |> Effect.wrap


type GetAllEmployeeContractsSideEffect =
    | GetAllEmployeeContractsSideEffect of contractId: ContractId * yearMonth: YearMonth
    interface ISideEffect<ContractId list>

let getAllEmployeeContracts contractId yearMonth =
    Effect.Of(GetAllEmployeeContractsSideEffect (contractId, yearMonth))
    |> Effect.wrap
