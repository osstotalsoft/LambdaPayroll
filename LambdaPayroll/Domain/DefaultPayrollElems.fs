
module DefaultPayrollElems

open ElemAlgebra
open System

//default elems
let daysInMonth: PayrollElem<int> =
    elem {
        let! (_, (YearMonth (year, month))) = PayrollElem.ask
        return DateTime.DaysInMonth (year, month)
    }

let yearMonth: PayrollElem<YearMonth> =
    elem {
        let! (_, yearMonth) = PayrollElem.ask
        return yearMonth
    }