module SamplePayrollScheme

open Core
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
    fun _ -> effect { return DateTime.Now |> Ok }

//Formula elems
let nuEsteActiv = not esteActiv
let esteContractPrincipalSiEsteActiv = esteContractPrincipal && esteActiv
let esteContractPrincipalSiNuEsteActiv = esteContractPrincipal && not esteActiv

let esteContractPrincipalSiEsteActivLunaTrecuta =
    (esteContractPrincipal && esteActiv) |> lastMonth

let esteContractPrincipalSiEsteActivAcum2Luni =
    (esteContractPrincipal && esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum2Luni =
    (esteContractPrincipal && not esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum3Luni =
    (esteContractPrincipal && not esteActiv)
    |> (3 |> monthsAgo)

let esteContractPrincipalSiAreToateContracteleActive =
    from allEmployeeContracts
    |> select (esteContractPrincipal && esteActiv)
    |> all

let esteContractPrincipalSiAreVreunContractInactivLunaTrecuta =
    from allEmployeeContracts
    |> select (esteContractPrincipal && not esteActiv)
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
    |> where (not esteContractPrincipal)
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
        (ceiling sumaImpozitelorNerotunjitePeToateContractele
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

let q (deductions:PayrollElem<int list>) = 
    elem {
        for x in deductions do
        where (x = (constant 0))
        select (x + (constant 0))//fun (x:PayrollElem<int>) -> x > (constant 0)
    }


// let q' (deductions:PayrollElem<int list>) = 
//     let xxx  = 
//         elem.Select(
//             elem.Where(
//                 elem.For(
//                     deductions, 
//                     (fun x -> elem.YieldFrom( x + (constant 1)))
//                 ), 
//                 (fun x-> x = (constant 0))
//             ),
//             (fun x -> x + (constant 0))
//         )
//     xxx
