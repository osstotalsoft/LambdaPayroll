module HrAdmin

open Core
open NBB.Core.Effects.FSharp


type HrInfo = {
    SalariuBrut: decimal
    EsteContractPrincipal: bool
    EsteActiv: bool
}

let hrDb = dict [
    YearMonth(2020, 7), dict [
        ContractId 1, {SalariuBrut=5000.00m; EsteContractPrincipal=true; EsteActiv=true}
        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
    ]
    YearMonth(2020, 6), dict [
        ContractId 1, {SalariuBrut=4000.00m; EsteContractPrincipal=true; EsteActiv=true}
        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
    ]
    YearMonth(2020, 5), dict [
        ContractId 1, {SalariuBrut=4000.00m; EsteContractPrincipal=true; EsteActiv=true}
        ContractId 11, {SalariuBrut=1000.00m; EsteContractPrincipal=false; EsteActiv=true}
        ContractId 101, {SalariuBrut=2000.00m; EsteContractPrincipal=false; EsteActiv=true}
    ]
]

let readFromDb<'a> (code:string) : PayrollElem<'a> =
    let cast (value:'b) = 
        if typeof<'a> = typeof<'b> then Ok (box value :?> 'a) else Error "Invalid elem type"

    fun contractId yearMonth ->
        effect {
            let hrInfo = (hrDb.Item yearMonth).Item contractId
            match code with
            | "salariuBrut" -> return cast hrInfo.SalariuBrut
            | "esteContractPrincipal" -> return cast hrInfo.EsteContractPrincipal
            | "esteActiv" -> return cast hrInfo.EsteActiv
            | _ -> return Error "Elem not found"
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

