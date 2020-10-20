module SamplePayrollScheme

open ElemAlgebra
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain

//HrAdmin elems
let salariuBrut =
    HrAdmin.readScalarFromDb<decimal>
        (ElemCode "salariuBrut")
        { TableName = "Salarii"
          ColumnName = "SalariuBrut" }

let esteContractPrincipal =
    HrAdmin.readScalarFromDb<bool>
        (ElemCode "esteContractPrincipal")
        { TableName = "Salarii"
          ColumnName = "EsteContractPrincipal" }

let esteActiv =
    HrAdmin.readScalarFromDb<bool>
        (ElemCode "esteActiv")
        { TableName = "Salarii"
          ColumnName = "EsteActiv" }


//payroll constants
let procentImpozit = constant 0.23456m //|> log "procentImpozit" |> memoize


//payroll lazy computed values
let now =
    PayrollElem.fromElemResult (effect { return DateTime.Now |> Ok })

//Formula elems
let nuEsteActiv = Not esteActiv
let esteContractPrincipalSiEsteActiv = esteContractPrincipal .&& esteActiv
let esteContractPrincipalSiNuEsteActiv = esteContractPrincipal .&& Not esteActiv

let esteContractPrincipalSiEsteActivLunaTrecuta =
    (esteContractPrincipal .&& esteActiv) |> lastMonth

let esteContractPrincipalSiEsteActivAcum2Luni =
    (esteContractPrincipal .&& esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum2Luni =
    (esteContractPrincipal .&& Not esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum3Luni =
    (esteContractPrincipal .&& Not esteActiv)
    |> (3 |> monthsAgo)

let esteContractPrincipalSiAreToateContracteleActive =
    from allEmployeeContracts
    |> select (esteContractPrincipal .&& esteActiv)
    |> all

let esteContractPrincipalSiAreVreunContractInactivLunaTrecuta =
    from allEmployeeContracts
    |> select (esteContractPrincipal .&& Not esteActiv)
    |> lastMonth
    |> any

let esteActivInToateUltimele3Luni =
    from 3 |> lastMonths |> select esteActiv |> all

let mediaSalariuluiBrutInUltimele3LuniActive =
    from 3
    |> lastMonths
    |> where esteActiv
    |> select salariuBrut
    |> avg

let impozitNerotunjit = procentImpozit * salariuBrut

let sumaImpozitelorNerotunjitePeToateContractele =
    from allEmployeeContracts
    |> select impozitNerotunjit
    |> sum

let sumaImpozitelorNerotunjitePeContracteleSecundare =
    from allEmployeeContracts
    |> where (Not esteContractPrincipal)
    |> select impozitNerotunjit
    |> sum

let sumaImpozitelorNerotunjitePeContracteleSecundare' =
    from allEmployeeContracts
    |> select (When esteContractPrincipal (constant 0m) impozitNerotunjit)
    |> sum

let sumaImpozitelorNerotunjitePeContracteleSecundare'' =
    from allEmployeeContracts
    |> select
        (When esteContractPrincipal
         <| Then(constant 0m)
         <| Else impozitNerotunjit)
    |> sum

let impozit =
    When
        esteContractPrincipal
        (ceil sumaImpozitelorNerotunjitePeToateContractele
         - sumaImpozitelorNerotunjitePeContracteleSecundare)
        impozitNerotunjit


let impoziteleNerotunjitePeToateContractele =
    from allEmployeeContracts
    |> select impozitNerotunjit

let impozitelePeToateContractele =
    from allEmployeeContracts |> select impozit

let sumaImpozitelorPeToateContractele =
    from allEmployeeContracts |> select impozit |> sum


let salariuNet = salariuBrut - impozit //|> log "salariuNet" |> memoize

let diferentaNetFataDeLunaTrecuta =
    salariuNet - (salariuNet |> from lastMonth)

let mediaSalariuluiNetPeUltimele3Luni =
    from 3 |> lastMonths |> select salariuNet |> avg


let ultimele3Luni = from 3 |> lastMonths |> select yearMonth

let q (deductions: PayrollElem<{|RangeStart: System.Decimal; RangeEnd: System.Decimal; Value: System.Decimal; DeductedPersonsCount: System.Decimal|} list>) =
    elem {
        let! x = mediaSalariuluiNetPeUltimele3Luni
        for d in deductions do
        where (d.DeductedPersonsCount > x)
        select d.Value
    }

let qq =
    elem {
        for contract in allEmployeeContracts do
        let! esteActiv = esteActiv @ contract
        where esteActiv
        let! sn = salariuNet @ contract
        select sn
    }

let qq' =
    elem {
        for contract in allEmployeeContracts do
        where' (esteActiv @ contract)
        select' (salariuNet @ contract)
    }
