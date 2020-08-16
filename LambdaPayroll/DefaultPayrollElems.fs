module DefaultPayrollElems

open Core
open NBB.Core.Effects.FSharp
open System

//default elems
let nrZileInLuna: PayrollElem<int> =
    fun ((ContractId _contractId), (YearMonth (year, month))) ->
        effect {
            return Result.Ok <| DateTime.DaysInMonth (year, month)
        }

let anLuna: PayrollElem<YearMonth> =
    fun ((ContractId _contractId), (YearMonth (year, month))) ->
        effect {
            return Result.Ok <| (YearMonth (year, month))
        }

