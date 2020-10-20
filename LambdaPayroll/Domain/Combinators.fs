module Combinators

#nowarn "86"

open NBB.Core.Effects.FSharp
open ElemAlgebra


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
            })

    let allEmployeeContracts: PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (contractId, yearMonth) ->
            effect {
                let! allContracts = HrAdmin.getAllEmployeeContracts contractId yearMonth

                let allContractsElemResults =
                    allContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! allContractsElemResults
            })

[<AutoOpen>]
module UtilityCombinators =
    let log elemCode (PayrollElem elem: PayrollElem<'a>) =
        fun ((ContractId contractId), (YearMonth (year, month))) ->
            effect {
                printfn
                    "*** LOG *** evaluating elem %A on contractId:%A year:%A month:%A "
                    elemCode
                    contractId
                    year
                    month
                return! elem ((ContractId contractId), (YearMonth(year, month)))
            }

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
module QueryCombinators =
    let from = id

    let select (selector: PayrollElem<'a>) (source: PayrollElem<PayrollElemContext list>): PayrollElem<'a list> =
        PayrollElem
            (run source
             >> PayrollElemResult.bind
                 (List.map (run selector)
                  >> PayrollElemResultList.sequence))

    let select' (selector: PayrollElem<'a> -> PayrollElem<'b>) (source: PayrollElem<'a list>): PayrollElem<'b list> =
        source
        |> PayrollElem.bind (PayrollElemList.traverse (PayrollElem.return' >> selector))

    let Then = id
    let Else = id

    let where (predicate: PayrollElem<bool>)
              (source: PayrollElem<PayrollElemContext list>)
              : PayrollElem<PayrollElemContext list> =
        PayrollElem(fun (ctx: PayrollElemContext) ->
            let mapping =
                fun ctx' ->
                    run predicate ctx'
                    |> PayrollElemResult.map (fun b -> (ctx', b))

            ctx
            |> run source
            |> (List.map >> PayrollElemResult.map) mapping
            |> PayrollElemResult.bind PayrollElemResultList.sequence
            |> PayrollElemResult.map (List.filter snd >> List.map fst))

    let where' (predicate: PayrollElem<'a> -> PayrollElem<bool>) (source: PayrollElem<'a list>): PayrollElem<'a list> =
        let tuple2 a b = a, b
        source
        |> PayrollElem.bind
            (PayrollElemList.traverse (fun b ->
                b
                |> PayrollElem.return'
                |> predicate
                |> PayrollElem.map (tuple2 b)))
        |> PayrollElem.map (List.filter snd >> List.map fst)