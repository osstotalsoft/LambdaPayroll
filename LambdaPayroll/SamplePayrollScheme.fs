﻿module SamplePayrollScheme

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
let nuEsteActiv = not' esteActiv
let esteContractPrincipalSiEsteActiv = esteContractPrincipal .&& esteActiv
let esteContractPrincipalSiNuEsteActiv = esteContractPrincipal .&& not' esteActiv

let esteContractPrincipalSiEsteActivLunaTrecuta =
    (esteContractPrincipal .&& esteActiv) |> lastMonth

let esteContractPrincipalSiEsteActivAcum2Luni =
    (esteContractPrincipal .&& esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum2Luni =
    (esteContractPrincipal .&& not' esteActiv)
    |> lastMonth
    |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum3Luni =
    (esteContractPrincipal .&& not' esteActiv)
    |> (3 |> monthsAgo)

let esteContractPrincipalSiAreToateContracteleActive =
    elem {
        for contract in allEmployeeContracts do
        all' ((esteContractPrincipal .&& esteActiv) @ contract)
    }

let esteContractPrincipalSiAreVreunContractInactivLunaTrecuta =
    elem {
        for contract in allEmployeeContracts do
        where' (esteContractPrincipal @ contract)
        any' (not' esteActiv |> lastMonth)
    }

let esteActivInToateUltimele3Luni =
    elem {
        for month in (3 |> lastMonths) do
        all' (esteActiv @ month)
    }

let mediaSalariuluiBrutInUltimele3LuniActive =
    elem {
        for month in (3 |> lastMonths) do
        where' (esteActiv @ month)
        averageBy' (salariuBrut @ month)
    }

let impozitNerotunjit = procentImpozit * salariuBrut

let sumaImpozitelorNerotunjitePeToateContractele =
    elem {
        for contract in allEmployeeContracts do
        sumBy' (impozitNerotunjit @ contract)
    }

let sumaImpozitelorNerotunjitePeContracteleSecundare =
    elem {
        for contract in allEmployeeContracts do
        where' (not' esteContractPrincipal @ contract)
        sumBy' (impozitNerotunjit @ contract)
    }

let impozit =
    when'
        esteContractPrincipal
        (ceil sumaImpozitelorNerotunjitePeToateContractele
         - sumaImpozitelorNerotunjitePeContracteleSecundare)
        impozitNerotunjit


let impoziteleNerotunjitePeToateContractele =
     elem {
        for contract in allEmployeeContracts do
        select' (impozitNerotunjit @ contract)
    }

let impozitelePeToateContractele =
    elem {
        for contract in allEmployeeContracts do
        select' (impozit @ contract)
    }

let sumaImpozitelorPeToateContractele =
    elem {
        for contract in allEmployeeContracts do
        sumBy' (impozit @ contract)
    }


let salariuNet = salariuBrut - impozit //|> log "salariuNet" |> memoize

let diferentaNetFataDeLunaTrecuta =
    salariuNet - (salariuNet |> lastMonth)

let mediaSalariuluiNetPeUltimele3Luni =
    elem {
        for month in (3 |> lastMonths) do
        where' (esteActiv @ month)
        averageBy' (salariuNet @ month)
    }


let ultimele3Luni = 
    elem {
        for month in (3 |> lastMonths) do
        select' (yearMonth @ month)
    }

let Deductions = 
    HrAdmin.readCollectionFromDb<{|RangeStart: System.Decimal; RangeEnd: System.Decimal; Value: System.Decimal; DeductedPersonsCount: System.Decimal|}>
            (ElemCode "Deductions") { 
                TableName = "hr.Deduction"
                Columns = [{ColumnName= "RangeStart"; ColumnDataType = "System.Decimal"}; {ColumnName= "RangeEnd"; ColumnDataType = "System.Decimal"}; {ColumnName= "Value"; ColumnDataType = "System.Decimal"}; {ColumnName= "DeductedPersonsCount"; ColumnDataType = "System.Decimal"}]}

[<AutoOpen>]
module Deductions =
    let inline _RangeStart a = PayrollElem.map (fun x -> (^a: (member RangeStart: _) x)) a
    let inline _RangeEnd a = PayrollElem.map (fun x -> (^a: (member RangeEnd: _) x)) a
    let inline _Value a = PayrollElem.map (fun x -> (^a: (member Value: _) x)) a
    let inline _DeductedPersonsCount a = PayrollElem.map (fun x -> (^a: (member DeductedPersonsCount: _) x)) a

    //     [<Extension>]
    //     type DeductionsExtensions =
    //         [<Extension>]
    //         static member inline RangeStart(a) = PayrollElem.map (fun x -> (^a: (member RangeStart: _) x)) a
    //         [<Extension>]
    //         static member inline RangeEnd(a) = PayrollElem.map (fun x -> (^a: (member RangeEnd: _) x)) a
    //         [<Extension>]
    //         static member inline Value(a) = PayrollElem.map (fun x -> (^a: (member Value: _) x)) a
    //         [<Extension>]
    //         static member inline DeductedPersonsCount(a) = PayrollElem.map (fun x -> (^a: (member DeductedPersonsCount: _) x)) a



let q (deductions: PayrollElem<{| RangeStart: System.Decimal
                                  RangeEnd: System.Decimal
                                  Value: System.Decimal
                                  DeductedPersonsCount: System.Decimal |} list>) =
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
