
module Generated1

open ElemAlgebra
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp
open LambdaPayroll.Domain

let ContractDeductedPersonsCount = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractDeductedPersonsCount") { TableName = "hr.Contract"; ColumnName = "DeductedPersonsCount" }

let AllContractsDeductedPersonsCount = 
    elem {
        for contract in allEmployeeContracts do
        maxBy' (ContractDeductedPersonsCount @ contract)
    }
    |> memoize

let ContractIsBasePosition = 
    HrAdmin.readScalarFromDb<Boolean> (ElemCode "ContractIsBasePosition") { TableName = "hr.Contract"; ColumnName = "IsBasePosition" }

let ContractGrossSalary = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "ContractGrossSalary") { TableName = "hr.Contract"; ColumnName = "GrossSalary" }

let ComputingPeriodWorkingDaysNo = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ComputingPeriodWorkingDaysNo") { TableName = "hr.ComputingPeriod"; ColumnName = "WorkingDaysNo" }

let ContractWorkingDayHours = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractWorkingDayHours") { TableName = "hr.Contract"; ColumnName = "WorkingDayHours" }

let HourWage = 
    ContractGrossSalary / decimal'(ComputingPeriodWorkingDaysNo * ContractWorkingDayHours)

let TimesheetTotalWorkedHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedHours" }

let IncomeForWorkedTime = round(HourWage * TimesheetTotalWorkedHours)
let TimesheetPayedAbsenceDays = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetPayedAbsenceDays") { TableName = "hr.Timesheet"; ColumnName = "PayedAbsenceDays" }

let IncomeForPaidAbsences = 
    round(HourWage * TimesheetPayedAbsenceDays * decimal'(ContractWorkingDayHours))

let TimesheetTotalWorkedOvertimeHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedOvertimeHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedOvertimeHours" }

let TimesheetOvertimeHoursFactor = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetOvertimeHoursFactor") { TableName = "hr.Timesheet"; ColumnName = "OvertimeHoursFactor" }

let IncomeForWorkedOverTime = 
    round(HourWage * TimesheetTotalWorkedOvertimeHours * TimesheetOvertimeHoursFactor)

let TotalGrossSalary = 
    IncomeForWorkedTime + IncomeForPaidAbsences + IncomeForWorkedOverTime

let baseAllContractsDeduction = 
    elem {
        for contract in allEmployeeContracts do
        where' (ContractIsBasePosition @ contract)
        sumBy' (TotalGrossSalary @ contract)
    } 
    |> memoize

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

let AllContractsDeduction = 
    elem {
        let! baseAllContractsDeduction' = baseAllContractsDeduction
        let! allContractsDeductedPersonsCount = AllContractsDeductedPersonsCount
        for d in Deductions do
        where (baseAllContractsDeduction' >=  d.RangeStart && baseAllContractsDeduction' <= d.RangeEnd)
        where (d.DeductedPersonsCount = decimal allContractsDeductedPersonsCount)
        maxBy d.Value
    }

let baseCAS = max' TotalGrossSalary (constant 0m)
let TaxCASPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASPct") { TableName = "hr.Tax"; ColumnName = "CASPct" }

let interimCAS = baseCAS * decimal' TaxCASPct / constant 100m
let CAS = 
    elem {
        let! interimCAS' = interimCAS
        return if interimCAS' > 0m && interimCAS' < 1m then 1m else round interimCAS'
    }

let baseCASS = min' TotalGrossSalary (constant 0m)
let TaxCASSPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASSPct") { TableName = "hr.Tax"; ColumnName = "CASSPct" }

let interimCASS = baseCASS * decimal' TaxCASSPct / constant 100m
let CASS = 
    elem {
        let! interimCASS' = interimCASS
        return if interimCASS' > 0m && interimCASS' < 1m then 1m else round interimCASS'
    }

let TaxImpozitPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxImpozitPct") { TableName = "hr.Tax"; ColumnName = "ImpozitPct" }

let baseDeduction = 
    when' ContractIsBasePosition TotalGrossSalary (constant 0m)
