
module Generated

open Core
open Combinators
open DefaultPayrollElems
open System
open NBB.Core.Effects.FSharp

let ContractDeductedPersonsCount = 
    HrAdmin.readFromDb<Int32> "ContractDeductedPersonsCount"

let AllContractsDeductedPersonsCount = 
    from allEmployeeContracts |> select ContractDeductedPersonsCount |> maxItem

let ContractGrossSalary = HrAdmin.readFromDb<Decimal> "ContractGrossSalary"
let ComputingPeriodWorkingDaysNo = 
    HrAdmin.readFromDb<Int32> "ComputingPeriodWorkingDaysNo"

let ContractWorkingDayHours = 
    HrAdmin.readFromDb<Int32> "ContractWorkingDayHours"

let HourWage = 
    ContractGrossSalary / decimal(ComputingPeriodWorkingDaysNo * ContractWorkingDayHours)

let TimesheetTotalWorkedHours = 
    HrAdmin.readFromDb<{|field1: Decimal; field2: int |}> "TimesheetTotalWorkedHours"

let IncomeForWorkedTime = ceiling(HourWage * TimesheetTotalWorkedHours)
let TimesheetPayedAbsenceDays = 
    HrAdmin.readFromDb<Decimal> "TimesheetPayedAbsenceDays"

let IncomeForPaidAbsences = 
    ceiling(HourWage * TimesheetPayedAbsenceDays * decimal(ContractWorkingDayHours))

let TimesheetTotalWorkedOvertimeHours = 
    HrAdmin.readFromDb<Decimal> "TimesheetTotalWorkedOvertimeHours"

let TimesheetOvertimeHoursFactor = 
    HrAdmin.readFromDb<Decimal> "TimesheetOvertimeHoursFactor"

let IncomeForWorkedOverTime = 
    ceiling(HourWage * TimesheetTotalWorkedOvertimeHours * TimesheetOvertimeHoursFactor)

let TotalGrossSalary = 
    IncomeForWorkedTime + IncomeForPaidAbsences + IncomeForWorkedOverTime

let baseCAS = max (Payroll.constant 0m) TotalGrossSalary
let TaxCASPct = HrAdmin.readFromDb<Int32> "TaxCASPct"
let interimCAS = 
    baseCAS * decimal(TaxCASPct) / (Payroll.constant 100m)

let CAS = 
    When (interimCAS > (Payroll.constant 0m) && interimCAS < (Payroll.constant 1m)) (Payroll.constant 1m) (ceiling interimCAS)

let TaxCASSPct = HrAdmin.readFromDb<Int32> "TaxCASSPct"
let interimCASS = 
    baseCAS * decimal(TaxCASSPct) / (Payroll.constant 100m)

let CASS = 
    When (interimCASS > (Payroll.constant 0m) && interimCASS < (Payroll.constant 1m)) (Payroll.constant 1m) (ceiling interimCASS)

let ContractIsBasePosition = 
    HrAdmin.readFromDb<Boolean> "ContractIsBasePosition"

let DeductionDeductedPersonsCount = 
    HrAdmin.readFromDb<Decimal> "DeductionDeductedPersonsCount"

let DeductionRangeEnd = HrAdmin.readFromDb<Decimal> "DeductionRangeEnd"
let DeductionRangeStart = HrAdmin.readFromDb<Decimal> "DeductionRangeStart"
let DeductionValue = HrAdmin.readFromDb<Decimal> "DeductionValue"
let baseAllContractsDeduction = 
    from allEmployeeContracts |> where (ContractIsBasePosition) |> select TotalGrossSalary |> sum

let baseCASS = max (Payroll.constant 0m) TotalGrossSalary
let x = from allEmployeeContracts |> where (DeductionRangeStart <= baseAllContractsDeduction && baseAllContractsDeduction  <= DeductionRangeEnd  && DeductionDeductedPersonsCount = decimal(AllContractsDeductedPersonsCount)) |> select DeductionValue |> maxItem