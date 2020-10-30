
module Generated'

open ElemAlgebra
open Combinators
open DefaultPayrollElems
open LambdaPayroll.Domain
open NBB.Core.Effects.FSharp
open System
open System.Runtime.CompilerServices  

let ContractGrossSalary = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "ContractGrossSalary") { TableName = "hr.Contract"; ColumnName = "GrossSalary" }

let ComputingPeriodWorkingDaysNo = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ComputingPeriodWorkingDaysNo") { TableName = "hr.ComputingPeriod"; ColumnName = "WorkingDaysNo" }

let ContractWorkingDayHours = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractWorkingDayHours") { TableName = "hr.Contract"; ColumnName = "WorkingDayHours" }

let TimesheetTotalWorkedHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedHours" }

let TimesheetPayedAbsenceDays = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetPayedAbsenceDays") { TableName = "hr.Timesheet"; ColumnName = "PayedAbsenceDays" }

let TimesheetTotalWorkedOvertimeHours = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetTotalWorkedOvertimeHours") { TableName = "hr.Timesheet"; ColumnName = "TotalWorkedOvertimeHours" }

let TimesheetOvertimeHoursFactor = 
    HrAdmin.readScalarFromDb<Decimal> (ElemCode "TimesheetOvertimeHoursFactor") { TableName = "hr.Timesheet"; ColumnName = "OvertimeHoursFactor" }

let TotalGrossSalary = 
    let hourWage = ContractGrossSalary / decimal'(ComputingPeriodWorkingDaysNo * ContractWorkingDayHours)
    let incomeForWorkedTime = round(hourWage * TimesheetTotalWorkedHours)
    let incomeForPaidAbsences = round(hourWage * TimesheetPayedAbsenceDays * decimal'(ContractWorkingDayHours))
    let incomeForWorkedOverTime = round(hourWage * TimesheetTotalWorkedOvertimeHours * TimesheetOvertimeHoursFactor)
    incomeForWorkedTime + incomeForPaidAbsences + incomeForWorkedOverTime

let TaxCASPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASPct") { TableName = "hr.Tax"; ColumnName = "CASPct" }

let CAS = 
    elem {
        let! totalGrossSalary = TotalGrossSalary
        let baseCAS = max totalGrossSalary 0m
        let! taxCASPct = TaxCASPct
        let interimCAS = baseCAS * decimal taxCASPct / 100m
        return if interimCAS > 0m && interimCAS < 1m then 1m else round interimCAS
    }

let TaxCASSPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxCASSPct") { TableName = "hr.Tax"; ColumnName = "CASSPct" }

let CASS = 
    elem {
        let! totalGrossSalary = TotalGrossSalary
        let baseCASS = min totalGrossSalary 0m
        let! taxCASSPct = TaxCASSPct
        let interimCASS = baseCASS * decimal taxCASSPct / 100m
        return if interimCASS > 0m && interimCASS < 1m then 1m else round interimCASS
    }

let ContractDeductedPersonsCount = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "ContractDeductedPersonsCount") { TableName = "hr.Contract"; ColumnName = "DeductedPersonsCount" }

let ContractIsBasePosition = 
    HrAdmin.readScalarFromDb<Boolean> (ElemCode "ContractIsBasePosition") { TableName = "hr.Contract"; ColumnName = "IsBasePosition" }

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
                Columns = [{ColumnName= "RangeStart"; ColumnDataType = "System.Decimal"}; {ColumnName= "RangeEnd"; ColumnDataType = "System.Decimal"}; {ColumnName= "Value"; ColumnDataType = "System.Decimal"}; {ColumnName= "DeductedPersonsCount"; ColumnDataType = "System.Decimal"}]}

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
    }

let TaxImpozitPct = 
    HrAdmin.readScalarFromDb<Int32> (ElemCode "TaxImpozitPct") { TableName = "hr.Tax"; ColumnName = "ImpozitPct" }

let Impozit = 
    elem {
        let baseImpozit = max' (TotalGrossSalary - CAS - CASS - Deduction) (constant 0m)
        let! interimImpozit = baseImpozit * decimal' TaxImpozitPct / constant 100m
        return 
            if interimImpozit > 0m && interimImpozit < 1m 
            then 1m
            else round interimImpozit
    }

let Net = TotalGrossSalary - CAS - CASS - Impozit