module Combinators

#nowarn "86"

open System
open NBB.Core.Effects.FSharp
open Core


[<AutoOpen>]
module HrCombinators =
    let lastMonth (elem: PayrollElem<'a>): PayrollElem<'a> =
        fun (contractId, yearMonth) -> elem (contractId, (YearMonth.lastMonth yearMonth))

    let monthsAgo (n: int) (elem: PayrollElem<'a>): PayrollElem<'a> =
        fun (contractId, yearMonth) -> elem (contractId, (YearMonth.subStractMonth n yearMonth))

    let lastNMonths (n: int): PayrollElem<PayrollElemContext list> =
        fun (contractId, yearMonth) ->
            [ 0 .. n - 1 ]
            |> List.map (fun x ->
                let yearMonth' = YearMonth.subStractMonth x yearMonth
                let ctx = (contractId, yearMonth')
                PayrollElemResult.return' ctx)
            |> List.sequencePayrollElemResult

    let inMonth (yearMonth: YearMonth) (elem: PayrollElem<'a>): PayrollElem<'a> =
        fun (contractId, _yearMonth) -> elem (contractId, yearMonth)


    let otherEmployeeContracts: PayrollElem<PayrollElemContext list> =
        fun (contractId, yearMonth) ->
            effect {
                let! otherContracts = HrAdmin.getOtherEmployeeContracts contractId

                let otherContractsElemResults =
                    otherContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! otherContractsElemResults
            }

    let allEmployeeContracts: PayrollElem<PayrollElemContext list> =
        fun (contractId, yearMonth) ->
            effect {
                let! allContracts = HrAdmin.getAllEmployeeContracts contractId

                let allContractsElemResults =
                    allContracts
                    |> List.map (fun x -> (x, yearMonth))
                    |> PayrollElemResult.return'

                return! allContractsElemResults
            }

[<AutoOpen>]
module NumericCombinators =
    let inline (+) (a: PayrollElem< ^a > when (^a or ^b): (static member (+): ^a * ^b -> ^c)) (b: PayrollElem< ^b >) =
        PayrollElem.lift2 (+) a b

    let inline (-) (a: PayrollElem< ^a > when (^a or ^b): (static member (-): ^a * ^b -> ^c)) (b: PayrollElem< ^b >) =
        PayrollElem.lift2 (-) a b

    let inline (*) (a: PayrollElem< ^a > when (^a or ^b): (static member (*): ^a * ^b -> ^c)) (b: PayrollElem< ^b >) =
        PayrollElem.lift2 (*) a b

    let inline (/) (a: PayrollElem< ^a > when (^a or ^b): (static member (/): ^a * ^b -> ^c)) (b: PayrollElem< ^b >) =
        PayrollElem.lift2 (/) a b


    let ceiling (a: PayrollElem<decimal>) =
        // fsharplint:disable-next-line
        PayrollElem.map (fun (d: decimal) -> Math.Ceiling d) a

    let inline sum (xs: PayrollElem< ^a list> when ^a: (static member (+): ^a * ^a -> ^a) and ^a: (static member Zero: ^a))
                   : PayrollElem< ^a > =
        xs |> PayrollElem.map List.sum

    let inline avg (xs: PayrollElem< ^a list> when ^a: (static member (+): ^a * ^a -> ^a)): PayrollElem< ^a > =
        xs |> PayrollElem.map List.average

[<AutoOpen>]
module BooleanCombinators =
    let When (cond: PayrollElem<bool>) (e1: PayrollElem<'a>) (e2: PayrollElem<'a>) =
        elem {
            let! cond' = cond
            if cond' then return! e1 else return! e2
        }

    let all = PayrollElem.map (List.reduce (&&))

    let any = PayrollElem.map (List.reduce (||))

    let (&&) = PayrollElem.lift2 (&&)
    let (||) = PayrollElem.lift2 (||)
    let (not) = PayrollElem.map (not)

[<AutoOpen>]
module UtilityCombinators =
    let log elemCode (elem: PayrollElem<'a>) =
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

    let memoize (elem: PayrollElem<'a>) =
        fun ((ContractId contractId), (YearMonth (year, month))) ->

            effect {
                let! cachedValue = ElemCache.get elem (ContractId contractId) (YearMonth(year, month))

                if cachedValue.IsSome then
                    return cachedValue.Value
                else
                    let! value = elem ((ContractId contractId), (YearMonth(year, month)))
                    do! ElemCache.set elem (ContractId contractId) (YearMonth(year, month)) value
                    return value
            }

[<AutoOpen>]
module QueryCombinators =
    let from = id

    let select (selector: PayrollElem<'a>) (source: PayrollElem<PayrollElemContext list>): PayrollElem<'a list> =
        source
        >> PayrollElemResult.bind
            (List.map selector
             >> List.sequencePayrollElemResult)

    let Then = id
    let Else = id

    let where (predicate: PayrollElem<bool>)
              (source: PayrollElem<PayrollElemContext list>)
              : PayrollElem<PayrollElemContext list> =
        fun (ctx: PayrollElemContext) ->
            let mapping =
                fun ctx' ->
                    predicate ctx'
                    |> PayrollElemResult.map (fun b -> (ctx', b))

            ctx
            |> source
            |> (List.map >> PayrollElemResult.map) mapping
            |> PayrollElemResult.bind List.sequencePayrollElemResult
            |> PayrollElemResult.map (List.filter snd >> List.map fst)
