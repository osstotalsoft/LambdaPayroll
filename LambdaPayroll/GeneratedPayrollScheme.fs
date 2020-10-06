
module Generated1

open Core
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain

let ContractDeductedPersonsCount = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractDeductedPersonsCount") { TableName = "hr.Contract"; ColumnName = "DeductedPersonsCount" }

let AllContractsDeductedPersonsCount = 
    from allEmployeeContracts |> select ContractDeductedPersonsCount |> max |> memoize

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

let ContractIsBasePosition = 
    HrAdmin.readScalarFromDb<Boolean> (ElemCode "ContractIsBasePosition") { TableName = "hr.Contract"; ColumnName = "IsBasePosition" }

let ContractGrossSalary = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "ContractGrossSalary") { TableName = "hr.Contract"; ColumnName = "GrossSalary" }

let ComputingPeriodWorkingDaysNo = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ComputingPeriodWorkingDaysNo") { TableName = "hr.ComputingPeriod"; ColumnName = "WorkingDaysNo" }

let ContractWorkingDayHours = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractWorkingDayHours") { TableName = "hr.Contract"; ColumnName = "WorkingDayHours" }

let HourWage = 
    ContractGrossSalary / decimal(ComputingPeriodWorkingDaysNo * ContractWorkingDayHours)

let TimesheetTotalWorkedHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedHours" }

let IncomeForWorkedTime = round(HourWage * TimesheetTotalWorkedHours)
let TimesheetPayedAbsenceDays = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetPayedAbsenceDays") { TableName = "hr.Timesheet"; ColumnName = "PayedAbsenceDays" }

let IncomeForPaidAbsences = 
    round(HourWage * TimesheetPayedAbsenceDays * decimal(ContractWorkingDayHours))

let TimesheetTotalWorkedOvertimeHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedOvertimeHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedOvertimeHours" }

let TimesheetOvertimeHoursFactor = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetOvertimeHoursFactor") { TableName = "hr.Timesheet"; ColumnName = "OvertimeHoursFactor" }

let IncomeForWorkedOverTime = 
    round(HourWage * TimesheetTotalWorkedOvertimeHours * TimesheetOvertimeHoursFactor)

let TotalGrossSalary = 
    IncomeForWorkedTime + IncomeForPaidAbsences + IncomeForWorkedOverTime

let baseAllContractsDeduction = 
    from allEmployeeContracts |> where (ContractIsBasePosition) |> select TotalGrossSalary |> sum |>memoize

let AllContractsDeduction = 
    from Deductions
        |> where' (fun d ->
            (baseAllContractsDeduction |> between ( d |> _RangeStart)  ( d |> _RangeEnd)) &&
            ( (d |> _DeductedPersonsCount) = decimal(AllContractsDeductedPersonsCount)))
        |> select' _Value
        |> max

let baseCAS = TotalGrossSalary |> minValue (constant 0m) 
let TaxCASPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASPct") { TableName = "hr.Tax"; ColumnName = "CASPct" }

let interimCAS = baseCAS * decimal(TaxCASPct) / (constant 100m)
let CAS = 
    When (interimCAS > (constant 0m) && interimCAS < (constant 1m)) (constant 1m) (round interimCAS)

let baseCASS = TotalGrossSalary |> minValue (constant 0m) 
let TaxCASSPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASSPct") { TableName = "hr.Tax"; ColumnName = "CASSPct" }

let interimCASS = baseCASS * decimal(TaxCASSPct) / (constant 100m)
let CASS = 
    When (interimCASS > (constant 0m) && interimCASS < (constant 1m)) (constant 1m) (round interimCASS)

let DeductionDeductedPersonsCount = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "DeductionDeductedPersonsCount") { TableName = "hr.Deduction"; ColumnName = "DeductedPersonsCount" }

let DeductionRangeEnd = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "DeductionRangeEnd") { TableName = "hr.Deduction"; ColumnName = "RangeEnd" }

let DeductionRangeStart = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "DeductionRangeStart") { TableName = "hr.Deduction"; ColumnName = "RangeStart" }

let DeductionValue = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "DeductionValue") { TableName = "hr.Deduction"; ColumnName = "Value" }

let TaxImpozitPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxImpozitPct") { TableName = "hr.Tax"; ColumnName = "ImpozitPct" }

let baseDeduction = 
    When ContractIsBasePosition TotalGrossSalary (constant 0m)
