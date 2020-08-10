module SamplePayrollScheme

open Core
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp

//HrAdmin elems
let salariuBrut = HrAdmin.readFromDb<decimal> "salariuBrut"
let esteContractPrincipal = HrAdmin.readFromDb<bool> "esteContractPrincipal"
let esteActiv = HrAdmin.readFromDb<bool> "esteActiv"


//payroll constants
let procentImpozit = Payroll.constant 0.23456m  |> log "procentImpozit" |> memoize


//payroll lazy computed values
let now = 
    fun _ _ -> effect {
        return DateTime.Now |> Ok
    }

//Formula elems
let nuEsteActiv = not esteActiv
let esteContractPrincipalSiEsteActiv = esteContractPrincipal && esteActiv
let esteContractPrincipalSiNuEsteActiv = esteContractPrincipal && not esteActiv
let esteContractPrincipalSiEsteActivLunaTrecuta = esteContractPrincipal && lastMonth esteActiv

let esteContractPrincipalSiEsteActivLunaTrecuta' = 
    select esteContractPrincipal && esteActiv
    |> from lastMonth

let esteContractPrincipalSiEsteActivAcum2Luni = 
    select esteContractPrincipal && esteActiv 
    |> from lastMonth |> lastMonth

let esteContractPrincipalSiNuEsteActivAcum2Luni = 
    select esteContractPrincipal && not esteActiv 
    |> from lastMonth 
    |> from lastMonth

let esteContractPrincipalSiNuEsteActivAcum3Luni = 
    select esteContractPrincipal && not esteActiv 
    |> from nMonthsAgo 3

let esteContractPrincipalSiAreToateContracteleActive = 
    select esteContractPrincipal && esteActiv 
    |> from allEmployeeContracts 
    |> all

let esteContractPrincipalSiAreVreunContractInactivLunaTrecuta = 
    select esteContractPrincipal && not esteActiv 
    |> from lastMonth 
    |> from allEmployeeContracts 
    |> any

let esteActivUltimele3Luni = 
    select esteActiv 
    |> from last_N_Months 3 
    |> all



let impozitNerotunjit = procentImpozit * salariuBrut
let sumaImpozitelorNerotunjitePeToateContractele = 
    select impozitNerotunjit 
    |> from allEmployeeContracts 
    |> sum

let sumaImpozitelorNerotunjitePeContracteleSecundare = 
    select When esteContractPrincipal 
        (constant 0m)
        impozitNerotunjit
    |> from allEmployeeContracts 
    |> sum

let sumaImpozitelorNerotunjitePeContracteleSecundare' = 
    select When esteContractPrincipal 
        <| Then (constant 0m)
        <| Else impozitNerotunjit
    |> from allEmployeeContracts 
    |> sum

let impozit = 
    When esteContractPrincipal
        (ceiling sumaImpozitelorNerotunjitePeToateContractele - sumaImpozitelorNerotunjitePeContracteleSecundare)
        impozitNerotunjit


let impoziteleNerotunjitePeToateContractele = impozitNerotunjit |> from allEmployeeContracts
let impozitelePeToateContractele = impozit |> allEmployeeContracts
let sumaImpozitelorPeToateContractele = impozit |> allEmployeeContracts |> sum
let sumaImpozitelorPeToateContractele' = sum (allEmployeeContracts impozit)



let salariuNet = salariuBrut - impozit |> log "salariuNet" |> memoize
let diferentaNetFataDeLunaTrecuta = salariuNet - (salariuNet |> from lastMonth)

let mediaSalariuluiNetPeUltimele3Luni = 
    select salariuNet 
    |> from last_N_Months 3
    |> avg


let ultimele3Luni = 
    select anLuna
    |> from last_N_Months 3


//let activInUltimele3Luni = ultimele3Luni >=> (fun luni -> luni |> List.map (fun luna -> salariuNet |> inMonth luna))



        




