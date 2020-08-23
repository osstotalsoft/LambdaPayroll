module HrAdmin

open Core
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain
open NBB.Core.Effects

type LoadSideEffect = {
    Definition: DbElemDefinition
    Context: PayrollElemContext
}
with interface ISideEffect<Result<obj, string>>

let load definition (contractId, yearMonth) = (Effect.Of {Definition=definition; Context=(contractId, yearMonth)}) |> Effect.wrap

//type HrInfo = {
//    SalariuBrut: decimal
//    EsteContractPrincipal: bool
//    EsteActiv: bool
//}

//let hrDb = dict [
//    YearMonth(2020, 7), dict [
//        ContractId 1, {SalariuBrut=5000.00m; EsteContractPrincipal=true; EsteActiv=true}
//        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
//        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
//    ]
//    YearMonth(2020, 6), dict [
//        ContractId 1, {SalariuBrut=4000.00m; EsteContractPrincipal=true; EsteActiv=true}
//        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
//        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
//    ]
//    YearMonth(2020, 5), dict [
//        ContractId 1, {SalariuBrut=4000.00m; EsteContractPrincipal=true; EsteActiv=true}
//        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
//        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
//    ]
//]

let readFromDb<'a> (code:string) : PayrollElem<'a> =
    let cast (value:'b) = 
        if typeof<'a> = value.GetType() then Ok (box value :?> 'a) else Error "Invalid elem type"

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

let getOtherEmployeeContracts (ContractId contractId): Effect<ContractId list> = 
    effect {
        return [
            ContractId (contractId + 10)
            ContractId (contractId + 100)
        ]
    }

let getAllEmployeeContracts (ContractId contractId): Effect<ContractId list> = 
    effect {
        return [
            ContractId contractId
            ContractId (contractId + 10)
            ContractId (contractId + 100)
        ]
    }

