module rec Core

open LambdaPayroll.Domain
open NBB.Core.Effects.FSharp

type PayrollElem<'a> = PayrollElemContext -> PayrollElemResult<'a>

and PayrollElemContext = ContractId * YearMonth

and ContractId = ContractId of int

and YearMonth = YearMonth of year: int * month: int

and PayrollElemResult<'a> = Effect<Result<'a, string>>


module YearMonth =
    let lastMonth (YearMonth (year, month)) = YearMonth(year, month - 1)
    let subStractMonth (n: int) (YearMonth (year, month)) = YearMonth(year, month - n)

module PayrollElemResult =
    let map (func: 'a -> 'b) (eff: PayrollElemResult<'a>): PayrollElemResult<'b> =
        eff
        |> Effect.map (fun result -> result |> Result.map func)

    let bind (func: 'a -> PayrollElemResult<'b>) (eff: PayrollElemResult<'a>): PayrollElemResult<'b> =
        effect {
            let! r = eff

            match r with
            | Ok x -> return! func x
            | Error err -> return Error err
        }

    let apply (func: PayrollElemResult<'a -> 'b>) (eff: PayrollElemResult<'a>) =
        bind (fun a -> func |> map (fun fn -> fn a)) eff

    let return' x = x |> Ok |> Effect.pure'

    let composeK f g x = bind g (f x)

    let lift2 f = map f >> apply

    let flatten eff = bind id eff

module PayrollElem =
    let map (func: 'a -> 'b) (eff: PayrollElem<'a>): PayrollElem<'b> =
        fun ctx -> eff ctx |> PayrollElemResult.map func

    let bind (func: 'a -> PayrollElem<'b>) (eff: PayrollElem<'a>): PayrollElem<'b> =
        fun ctx ->
            eff ctx
            |> PayrollElemResult.bind (fun a -> func a ctx)

    let apply (func: PayrollElem<'a -> 'b>) (eff: PayrollElem<'a>) =
        bind (fun a -> func |> map (fun fn -> fn a)) eff

    let return' (x: 'a): PayrollElem<'a> = fun _ctx -> PayrollElemResult.return' x

    let composeK f g x = bind g (f x)

    let lift2 f = map f >> apply
    let lift3 f a = lift2 f a >> apply

    let flatten eff = bind id eff


type PayrollElemList<'a> = PayrollElem<list<'a>>

module PayrollElemList =
    
    let map (func: 'a -> 'b) (elemList: PayrollElemList<'a>): PayrollElemList<'b> =
        elemList |> PayrollElem.map (List.map func)

    let bind (func: 'a -> PayrollElemList<'b>) (elemList: PayrollElemList<'a>): PayrollElemList<'b> =
        elemList
        |> PayrollElem.bind
            ((List.map func)
             >> List.sequencePayrollElem
             >> PayrollElem.map List.flatten)

    let return' (x: 'a): PayrollElemList<'a> = PayrollElem.return' [ x ]

    let hoist (list: 'a list): PayrollElem<'a list> = PayrollElem.return' list
    let lift (elem: PayrollElem<'a>): PayrollElemList<'a> = elem |> PayrollElem.map (fun x -> [ x ])


    let filter (predicate: 'a -> PayrollElem<bool>) (coll: PayrollElemList<'a>): PayrollElemList<'a> =
        let (<*>) = PayrollElem.apply
        let return' = PayrollElem.return'
        let initState = hoist []
        let consIf cond head tail = if cond then head :: tail else tail

        let folder (head: 'a) (tail: PayrollElemList<'a>) =
            return' consIf
            <*> (predicate head)
            <*> (return' head)
            <*> tail

        coll
        |> PayrollElem.bind (fun list -> List.foldBack folder list initState)


module PayrollElemBuilder =
    type PayrollElemBuilder() =
        member _.Bind(eff, func) = eff |> PayrollElem.bind func
        member _.Return(value) = PayrollElem.return' value
        member _.ReturnFrom(value) = value
        member _.Combine(eff1, eff2) = eff1 |> PayrollElem.bind (fun _ -> eff2)
        member _.Zero() = PayrollElem.return' ()

        member _.For(coll, f) =
            coll
            |> PayrollElemList.bind (PayrollElem.return' >> f)

        member _.Yield(value) = PayrollElemList.lift value

        member _.YieldFrom(x) = x

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member _.Where(coll, [<ProjectionParameter>] predicate) =
            coll
            |> PayrollElemList.filter (PayrollElem.return' >> predicate)

        [<CustomOperation("select")>]
        member _.Select(coll, [<ProjectionParameter>] f) =
            coll
            |> PayrollElem.bind (List.traversePayrollElem (PayrollElem.return' >> f))

[<AutoOpen>]
module PayrollElems =
    let elem = PayrollElemBuilder.PayrollElemBuilder()

    let (<!>) = PayrollElem.map
    let (<*>) = PayrollElem.apply
    let (>>=) eff func = PayrollElem.bind func eff
    let (>=>) = PayrollElem.composeK

    let constant = PayrollElem.return'

    let eval (elem: PayrollElem<'a>) ctx =
        effect {
            let! result = elem ctx

            match result with
            | Ok a -> return sprintf "%A" a
            | Error err -> return sprintf "Eroare: %s" err
        }

    let (@@) (elem: PayrollElem<'a>) (ctx: PayrollElem<PayrollElemContext>): PayrollElem<'a> =
        ctx >>= (fun ctx _ -> elem ctx)

[<RequireQualifiedAccess>]
module List =
    let traversePayrollElem f list =
        let (<*>) = PayrollElem.apply
        let cons head tail = head :: tail
        let initState = PayrollElem.return' []

        let folder head tail =
            PayrollElem.return' cons <*> (f head) <*> tail

        List.foldBack folder list initState

    let sequencePayrollElem list = traversePayrollElem id list

    let traversePayrollElemResult f list =
        let (<*>) = PayrollElemResult.apply
        let cons head tail = head :: tail
        let initState = PayrollElemResult.return' []

        let folder head tail =
            PayrollElemResult.return' cons
            <*> (f head)
            <*> tail

        List.foldBack folder list initState

    let sequencePayrollElemResult list = traversePayrollElemResult id list
