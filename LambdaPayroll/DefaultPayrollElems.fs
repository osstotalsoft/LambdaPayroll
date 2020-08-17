module DefaultPayrollElems

open Core
open NBB.Core.Effects.FSharp
open System

//default elems
let daysInMonth: PayrollElem<int> =
    fun ((ContractId _contractId), (YearMonth (year, month))) ->
        effect {
            return Result.Ok <| DateTime.DaysInMonth (year, month)
        }

let yearMonth: PayrollElem<YearMonth> =
    fun ((ContractId _contractId), (YearMonth (year, month))) ->
        effect {
            return Result.Ok <| (YearMonth (year, month))
        }

