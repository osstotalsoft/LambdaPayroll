module Combinators

#nowarn "86"

open NBB.Core.Effects.FSharp
open ElemAlgebra


[<AutoOpen>]
module UtilityCombinators =
    let log elemCode (elem: PayrollElem<'a>) =
        PayrollElem(fun ((ContractId contractId), (YearMonth (year, month))) ->
            effect {
                printfn
                    "*** LOG *** evaluating elem %A on contractId:%A year:%A month:%A "
                    elemCode
                    contractId
                    year
                    month
                return! run elem ((ContractId contractId), (YearMonth(year, month)))
            })

    let measure (elem: PayrollElem<'a>) =
        PayrollElem(fun ((ContractId contractId), (YearMonth (year, month))) ->
            effect {
                let stopWatch = System.Diagnostics.Stopwatch.StartNew()

                let! value = run elem ((ContractId contractId), (YearMonth(year, month)))

                stopWatch.Stop()
                printfn "Elapsed: %f ms" stopWatch.Elapsed.TotalMilliseconds
                return value
            })

    let memoize (elem: PayrollElem<'a>): PayrollElem<'a> =
        PayrollElem(fun (ctx: PayrollElemContext) ->
            effect {
                let! cachedValue = ElemCache.get elem ctx

                if cachedValue.IsSome then
                    return cachedValue.Value
                else
                    let! value = run elem ctx
                    do! ElemCache.set elem ctx value
                    return value
            })

[<AutoOpen>]
module HrCombinators =
    let lastMonth (elem: PayrollElem<'a>): PayrollElem<'a> =
        PayrollElem(fun (contractId, yearMonth) -> 
            run elem (contractId, (YearMonth.lastMonth yearMonth))
        )

    let monthsAgo (n: int) (elem: PayrollElem<'a>): PayrollElem<'a> =
        PayrollElem(fun (contractId, yearMonth) -> 
            run elem (contractId, (YearMonth.subStractMonth n yearMonth))
    )

    let lastMonths (n: int): PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (contractId, yearMonth) ->
            [ 0 .. n - 1 ]
            |> List.map (fun x ->
                let yearMonth' = YearMonth.subStractMonth x yearMonth
                let ctx = (contractId, yearMonth')
                PayrollElemResult.return' ctx)
            |> PayrollElemResultList.sequence)

    let inMonth (yearMonth: YearMonth) (elem: PayrollElem<'a>): PayrollElem<'a> =
        PayrollElem(fun (contractId, _yearMonth) -> run elem (contractId, yearMonth))


    let otherEmployeeContracts: PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (contractId, yearMonth) ->
            effect {
                let! otherContracts = HrAdmin.getOtherEmployeeContracts contractId

                let otherContractsElemResults =
                    otherContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! otherContractsElemResults
            }) |> memoize

    let allEmployeeContracts: PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (contractId, yearMonth) ->
            effect {
                let! allContracts = HrAdmin.getAllEmployeeContracts contractId yearMonth

                let allContractsElemResults =
                    allContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! allContractsElemResults
            }) |> memoize

    let allCompanyContracts: PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (_contractId, yearMonth) ->
            effect {
                let! allContracts = HrAdmin.getAllCompanyContracts yearMonth

                let allContractsElemResults =
                    allContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! allContractsElemResults
            }) |> memoize
