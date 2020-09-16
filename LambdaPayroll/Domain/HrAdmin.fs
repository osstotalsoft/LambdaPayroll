module HrAdmin

open Core
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain
open NBB.Core.Effects

type LoadScalarSideEffect =
    { Definition: DbScalarElemDefinition
      Context: PayrollElemContext }
    interface ISideEffect<Result<obj, string>>

let loadScalar definition (contractId, yearMonth) =
    (Effect.Of
        { Definition = definition
          Context = (contractId, yearMonth) })
    |> Effect.wrap

type LoadCollectionSideEffect =
    { Definition: DbCollectionElemDefinition
      Context: PayrollElemContext }
    interface ISideEffect<Result<obj[] list, string>>

let loadCollection<'a> definition (contractId, yearMonth) : Effect<Result<obj[] list, string>> =
    (Effect.Of
        { Definition = definition
          Context = (contractId, yearMonth) })
    |> Effect.wrap


let readScalarFromDb<'a> (ElemCode code) (dbScalarElemDefinition: DbScalarElemDefinition): PayrollElem<'a> =
    
    let cast (code: string) (value: 'b) =
        if isNull value then Error <| sprintf "Value for %s not found in HR DB" code
        else if typeof<'a> = value.GetType() then Ok(box value :?> 'a)
        else Error <| sprintf "Invalid elem type for %s (expected %s and received %s)" code (typeof<'a>).Name (value.GetType().Name)

    fun (contractId, yearMonth) ->
        effect {
            let! result = loadScalar dbScalarElemDefinition (contractId, yearMonth)
            return result |> Result.bind (cast code)
        }

let readCollectionFromDb<'a> (ElemCode code) (dbCollectionElemDefinition: DbCollectionElemDefinition): PayrollElem<'a list> =
    
    let cast (code: string) (value: 'b) =
        if isNull value then Error <| sprintf "Value for %s not found in HR DB" code
        else if typeof<'a> = value.GetType() then Ok(box value :?> 'a)
        else Error <| sprintf "Invalid elem type for %s (expected %s and received %s)" code (typeof<'a>).Name (value.GetType().Name)

    fun (contractId, yearMonth) ->
        effect {
            let! result = loadCollection dbCollectionElemDefinition (contractId, yearMonth)
            return result |> Result.map (List.map (fun vals -> FSharp.Reflection.FSharpValue.MakeRecord (typeof<'a>, vals) |> unbox<'a>))
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
