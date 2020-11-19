
module Generated'

open ElemAlgebra
open Combinators
open DefaultPayrollElems
open LambdaPayroll.Domain
open NBB.Core.Effects.FSharp
open System
open System.Runtime.CompilerServices  


type DailyEmployeeWorkingHoursRecord = 
    { Day: System.Int32;
      WorkingHours: System.Decimal }

[<Extension>]
type DailyEmployeeWorkingHoursExtensions =
    [<Extension>]
    static member Day (a: PayrollElem<DailyEmployeeWorkingHoursRecord>) = PayrollElem.map (fun x -> x.Day) a
    [<Extension>]
    static member WorkingHours (a: PayrollElem<DailyEmployeeWorkingHoursRecord>) = PayrollElem.map (fun x -> x.WorkingHours) a

let DailyEmployeeWorkingHours = 
    HrAdmin.readCollectionFromDb<DailyEmployeeWorkingHoursRecord>
            (ElemCode "DailyEmployeeWorkingHours") { 
                TableName = "hr.DailyEmployeeWorkingHours"
                Columns = [{ColumnName= "Day"; ColumnDataType = "System.Int32"}; {ColumnName= "WorkingHours"; ColumnDataType = "System.Decimal"}]} |> memoize 

let AverageEmployeesCnt = 
    elem {
            let getWorkingHours day = elem {
                for x in DailyEmployeeWorkingHours do
                where (x.Day = day)
                select x.WorkingHours
                headOrDefault
            }
            let referenceNorm = constant 8m
            let activeEmplAtDay day = elem {
                let wh = getWorkingHours day
                for ctr in allCompanyContracts do
                sumBy' (wh / referenceNorm @ ctr)
            }
            let! daysInMonth = daysInMonth
            let daysInMonthList = constant [1..daysInMonth]
            for day in daysInMonthList do
            averageBy' (activeEmplAtDay day)
        } |> memoize 

let TaxCASPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASPct") { TableName = "hr.Tax"; ColumnName = "CASPct" } |> memoize 

let ComputingPeriodWorkingDaysNo = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ComputingPeriodWorkingDaysNo") { TableName = "hr.ComputingPeriod"; ColumnName = "WorkingDaysNo" } |> memoize 

let ContractGrossSalary = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "ContractGrossSalary") { TableName = "hr.Contract"; ColumnName = "GrossSalary" } |> memoize 

let ContractWorkingDayHours = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractWorkingDayHours") { TableName = "hr.Contract"; ColumnName = "WorkingDayHours" } |> memoize 

let TimesheetOvertimeHoursFactor = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetOvertimeHoursFactor") { TableName = "hr.Timesheet"; ColumnName = "OvertimeHoursFactor" } |> memoize 

let TimesheetPayedAbsenceDays = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetPayedAbsenceDays") { TableName = "hr.Timesheet"; ColumnName = "PayedAbsenceDays" } |> memoize 

let TimesheetTotalWorkedHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedHours" } |> memoize 

let TimesheetTotalWorkedOvertimeHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedOvertimeHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedOvertimeHours" } |> memoize 

let TotalGrossSalary = 
    let hourWage = ContractGrossSalary / decimal'(ComputingPeriodWorkingDaysNo * ContractWorkingDayHours)
    let incomeForWorkedTime = round(hourWage * TimesheetTotalWorkedHours)
    let incomeForPaidAbsences = round(hourWage * TimesheetPayedAbsenceDays * decimal'(ContractWorkingDayHours))
    let incomeForWorkedOverTime = round(hourWage * TimesheetTotalWorkedOvertimeHours * TimesheetOvertimeHoursFactor)
    incomeForWorkedTime + incomeForPaidAbsences + incomeForWorkedOverTime |> memoize 

let CAS = 
    elem {
        let! totalGrossSalary = TotalGrossSalary
        let baseCAS = max totalGrossSalary 0m
        let! taxCASPct = TaxCASPct
        let interimCAS = baseCAS * decimal taxCASPct / 100m
        return if interimCAS > 0m && interimCAS < 1m then 1m else round interimCAS
    } |> memoize 

let TaxCASSPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASSPct") { TableName = "hr.Tax"; ColumnName = "CASSPct" } |> memoize 

let CASS = 
    elem {
        let! totalGrossSalary = TotalGrossSalary
        let baseCASS = min totalGrossSalary 0m
        let! taxCASSPct = TaxCASSPct
        let interimCASS = baseCASS * decimal taxCASSPct / 100m
        return if interimCASS > 0m && interimCASS < 1m then 1m else round interimCASS
    } |> memoize 

let ContractDeductedPersonsCount = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractDeductedPersonsCount") { TableName = "hr.Contract"; ColumnName = "DeductedPersonsCount" } |> memoize 

let ContractIsBasePosition = 
    HrAdmin.readScalarFromDb<Boolean> (ElemCode "ContractIsBasePosition") { TableName = "hr.Contract"; ColumnName = "IsBasePosition" } |> memoize 

type DeductionsRecord = 
    { RangeStart: System.Decimal;
      RangeEnd: System.Decimal;
      Value: System.Decimal;
      DeductedPersonsCount: System.Decimal }

[<Extension>]
type DeductionsExtensions =
    [<Extension>]
    static member RangeStart (a: PayrollElem<DeductionsRecord>) = PayrollElem.map (fun x -> x.RangeStart) a
    [<Extension>]
    static member RangeEnd (a: PayrollElem<DeductionsRecord>) = PayrollElem.map (fun x -> x.RangeEnd) a
    [<Extension>]
    static member Value (a: PayrollElem<DeductionsRecord>) = PayrollElem.map (fun x -> x.Value) a
    [<Extension>]
    static member DeductedPersonsCount (a: PayrollElem<DeductionsRecord>) = PayrollElem.map (fun x -> x.DeductedPersonsCount) a

let Deductions = 
    HrAdmin.readCollectionFromDb<DeductionsRecord>
            (ElemCode "Deductions") { 
                TableName = "hr.Deduction"
                Columns = [{ColumnName= "RangeStart"; ColumnDataType = "System.Decimal"}; {ColumnName= "RangeEnd"; ColumnDataType = "System.Decimal"}; {ColumnName= "Value"; ColumnDataType = "System.Decimal"}; {ColumnName= "DeductedPersonsCount"; ColumnDataType = "System.Decimal"}]} |> memoize 

let Deduction = 
    elem {
        let baseAllContractsDeduction = 
            elem {
                for contract in allEmployeeContracts do
                where' (ContractIsBasePosition @ contract)
                sumBy' (TotalGrossSalary @ contract)
            }
        let! allContractsDeductedPersonsCount = 
            elem {
                for contract in allEmployeeContracts do
                maxBy' (ContractDeductedPersonsCount @ contract)
            }
        let allContractsDeduction = 
            elem {
                let! baseAllContractsDeduction = baseAllContractsDeduction
                for d in Deductions do
                where (baseAllContractsDeduction >=  d.RangeStart && baseAllContractsDeduction <= d.RangeEnd)
                where (d.DeductedPersonsCount = decimal allContractsDeductedPersonsCount)
                maxBy d.Value
            }
        let baseDeduction = when' ContractIsBasePosition TotalGrossSalary (constant 0m)
        let deduction =  round (allContractsDeduction * baseDeduction / baseAllContractsDeduction)
        let! lastContractId = 
            elem {
                for contract in allEmployeeContracts do
                select' (contractId @ contract)
                last
            }
        let isLastContract = 
            elem {            
                let! currentContractId =  contractId @@ PayrollElem.ask
                return currentContractId = lastContractId
            }
        let sumDeductionAllContractsWithoutLastOne = 
            elem {
                for contract in allEmployeeContracts do
                where' (not' (isLastContract @ contract))
                sumBy' (deduction @ contract)
            }
        return! when' isLastContract 
            (allContractsDeduction - sumDeductionAllContractsWithoutLastOne)
            deduction
    } |> memoize 

let TaxImpozitPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxImpozitPct") { TableName = "hr.Tax"; ColumnName = "ImpozitPct" } |> memoize 

let Impozit = 
    elem {
        let baseImpozit = max' (TotalGrossSalary - CAS - CASS - Deduction) (constant 0m)
        let! interimImpozit = baseImpozit * decimal' TaxImpozitPct / constant 100m
        return 
            if interimImpozit > 0m && interimImpozit < 1m 
            then 1m
            else round interimImpozit
    } |> memoize 

let Net = 
    TotalGrossSalary - CAS - CASS - Impozit |> memoize 
