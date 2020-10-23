module ElemAlgebra

open LambdaPayroll.Domain
open NBB.Core.Effects.FSharp

type PayrollElem<'a> = PayrollElem of (PayrollElemContext -> PayrollElemResult<'a>)
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
    let run (PayrollElem elem) = elem
    let fromElemResult r = PayrollElem(fun _ -> r)
    let fromResult r = r |> Effect.pure' |> fromElemResult
    let map (func: 'a -> 'b) (elem: PayrollElem<'a>): PayrollElem<'b> =
        PayrollElem(run elem >> PayrollElemResult.map func)

    let bind (func: 'a -> PayrollElem<'b>) (eff: PayrollElem<'a>): PayrollElem<'b> =
        PayrollElem(fun ctx ->
            run eff ctx
            |> PayrollElemResult.bind (fun a -> run (func a) ctx))

    let apply (func: PayrollElem<'a -> 'b>) (eff: PayrollElem<'a>) =
        bind (fun a -> func |> map (fun fn -> fn a)) eff

    let return' (x: 'a): PayrollElem<'a> =
        PayrollElem(fun _ -> PayrollElemResult.return' x)

    let composeK f g x = bind g (f x)

    let lift2 f = map f >> apply
    let lift3 f a = lift2 f a >> apply

    let flatten eff = bind id eff

    let ask = PayrollElem PayrollElemResult.return'

type PayrollElem<'a> with
    static member inline (+) (a, b) = PayrollElem.lift2 (+) a b
    static member inline (-) (a, b) = PayrollElem.lift2 (-) a b
    static member inline (*) (a, b) = PayrollElem.lift2 (*) a b
    static member inline (/) (a, b) = PayrollElem.lift2 (/) a b
    static member inline (.>) (a, b) = PayrollElem.lift2 (>) a b
    static member inline (.>=) (a, b) = PayrollElem.lift2 (>=) a b
    static member inline (.<) (a, b) = PayrollElem.lift2 (<) a b
    static member inline (.<=) (a, b) = PayrollElem.lift2 (<=) a b
    static member inline (.=) (a, b) = PayrollElem.lift2 (=) a b
    static member inline Round(a) = PayrollElem.map round a
    static member inline Ceiling (a) = PayrollElem.map ceil a
    static member inline (.&&) (a, b) = PayrollElem.lift2 (&&) a b
    static member inline (.||) (a,b) = PayrollElem.lift2 (||) a b

type PayrollElemList<'a> = PayrollElem<list<'a>>

[<RequireQualifiedAccess>]
module PayrollElemList =
    let map (func: 'a -> 'b) (elemList: PayrollElemList<'a>): PayrollElemList<'b> =
        elemList |> PayrollElem.map (List.map func)

    let traverse f list =
        let (<*>) = PayrollElem.apply
        let cons head tail = head :: tail
        let initState = PayrollElem.return' []

        let folder head tail =
            PayrollElem.return' cons <*> (f head) <*> tail

        List.foldBack folder list initState

    let sequence list = traverse id list

    let bind (func: 'a -> PayrollElemList<'b>) (elemList: PayrollElemList<'a>): PayrollElemList<'b> =
        elemList
        |> PayrollElem.bind
            ((List.map func)
             >> sequence
             >> PayrollElem.map List.flatten)

    let return' (x: 'a): PayrollElemList<'a> = PayrollElem.return' [ x ]

    let hoist (list: 'a list): PayrollElem<'a list> = PayrollElem.return' list
    let lift (elem: PayrollElem<'a>): PayrollElemList<'a> = elem |> PayrollElem.map (fun x -> [ x ])

    let filter (predicate: 'a -> bool) (coll: PayrollElemList<'a>): PayrollElemList<'a> =
        coll |> PayrollElem.map (List.filter predicate)

    let filterElem (predicate: 'a -> PayrollElem<bool>) (coll: PayrollElemList<'a>): PayrollElemList<'a> =
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

[<RequireQualifiedAccess>]
module PayrollElemResultList =
    let traverse f list =
        let (<*>) = PayrollElemResult.apply
        let cons head tail = head :: tail
        let initState = PayrollElemResult.return' []

        let folder head tail =
            PayrollElemResult.return' cons
            <*> (f head)
            <*> tail

        List.foldBack folder list initState

    let sequence list = traverse id list

module PayrollElemBuilder =
    type PayrollElemBuilder() =
        member _.Bind(eff, func) = eff |> PayrollElem.bind func
        member _.Return(value) = PayrollElem.return' value
        member _.ReturnFrom(value) = value
        member _.Combine(eff1, eff2) = eff1 |> PayrollElem.bind (fun _ -> eff2)
        member _.Zero() = PayrollElem.return' ()

        member _.For(coll, f) =
            coll
            |> PayrollElemList.bind f

        member _.Yield(value) = PayrollElemList.return' value

        member _.YieldFrom(x) = PayrollElemList.lift x

        [<CustomOperation("where", MaintainsVariableSpace = true)>]
        member _.Where(coll, [<ProjectionParameter>] predicate) =
            coll
            |> PayrollElemList.filter predicate

        [<CustomOperation("where'", MaintainsVariableSpace = true)>]
        member _.WhereElem(coll, [<ProjectionParameter>] predicate) =
            coll
            |> PayrollElemList.filterElem predicate

        [<CustomOperation("select")>]
        member _.Select(coll, [<ProjectionParameter>] f) =
            coll
            |> PayrollElemList.map f

        [<CustomOperation("select'")>]
        member _.Select'(coll, [<ProjectionParameter>] f) =
            coll
            |> PayrollElem.bind (PayrollElemList.traverse f)

        [<CustomOperation("all")>]
        member this.All(coll, [<ProjectionParameter>] f) = 
            this.Select(coll, f) |> PayrollElem.map (List.reduce (&&))
        
        [<CustomOperation("all'")>]
        member this.All'(coll, [<ProjectionParameter>] f) = 
            this.Select'(coll, f) |> PayrollElem.map (List.reduce (&&))

        [<CustomOperation("any")>]
        member this.Any(coll, [<ProjectionParameter>] f) = 
            this.Select(coll, f) |> PayrollElem.map (List.reduce (||))
        
        [<CustomOperation("any'")>]
        member this.Any'(coll, [<ProjectionParameter>] f) = 
            this.Select'(coll, f) |> PayrollElem.map (List.reduce (||))

        [<CustomOperation("averageBy")>]
        member inline this.AverageBy(xs, [<ProjectionParameter>] f) = 
            this.Select(xs, f) |> PayrollElem.map List.average

        [<CustomOperation("averageBy'")>]
        member inline this.AverageBy'(xs, [<ProjectionParameter>] f) = 
            this.Select'(xs, f) |> PayrollElem.map List.average

        [<CustomOperation("sumBy")>]
        member inline this.SumBy(xs, [<ProjectionParameter>] f) = 
            this.Select(xs, f) |> PayrollElem.map List.sum

        [<CustomOperation("sumBy'")>]
        member inline this.SumBy'(xs, [<ProjectionParameter>] f) = 
            this.Select'(xs, f) |> PayrollElem.map List.sum

        [<CustomOperation("maxBy")>]
        member inline this.MaxBy(xs, [<ProjectionParameter>] f) = 
            this.Select(xs, f) |> PayrollElem.map List.max

        [<CustomOperation("maxBy'")>]
        member inline this.MaxBy'(xs, [<ProjectionParameter>] f) = 
            this.Select'(xs, f) |> PayrollElem.map List.max

        [<CustomOperation("last")>]
        member inline _.Last(xs) = 
            xs |> PayrollElem.map List.last

[<AutoOpen>]
module PayrollElems =
    let elem = PayrollElemBuilder.PayrollElemBuilder()

    let (<!>) = PayrollElem.map
    let (<*>) = PayrollElem.apply
    let (>>=) eff func = PayrollElem.bind func eff
    let (>=>) = PayrollElem.composeK

    let constant = PayrollElem.return'
    let run = PayrollElem.run
    let eval (elem: PayrollElem<'a>) ctx =
        effect {
            let! result = run elem ctx

            match result with
            | Ok a -> return sprintf "%A" a
            | Error err -> return sprintf "Eroare: %s" err
        }

    

    let (@) (elem: PayrollElem<'a>) (ctx: PayrollElemContext): PayrollElem<'a> =
        PayrollElem(fun _ -> run elem ctx)

    let (@@) (elem: PayrollElem<'a>) (ctx: PayrollElem<PayrollElemContext>): PayrollElem<'a> =
        ctx >>= fun ctx -> PayrollElem(fun _ -> run elem ctx)


[<AutoOpen>]
module NumericCombinators =
    let inline decimal' a = PayrollElem.map (decimal) a
    let inline max' a b = PayrollElem.lift2 max a b
    let inline min' a b = PayrollElem.lift2 min a b

    let inline between a b value = (a <= value) && (value <= b)
    let inline between' a b value = PayrollElem.lift3 between a b value


[<AutoOpen>]
module BooleanCombinators =
    let when' (cond: PayrollElem<bool>) (e1: PayrollElem<'a>) (e2: PayrollElem<'a>) =
        elem {
            let! cond' = cond
            if cond' then return! e1 else return! e2
        }

    let not' = PayrollElem.map (not)

// [<AutoOpen>]
// module ListCombinators = 
//     let inline sum (xs: PayrollElem< ^a list> when ^a: (static member (+): ^a * ^a -> ^a) and ^a: (static member Zero: ^a))
//                    : PayrollElem< ^a > =
//         xs |> PayrollElem.map List.sum

//     let inline avg (xs: PayrollElem< ^a list> when ^a: (static member (+): ^a * ^a -> ^a)): PayrollElem< ^a > =
//         xs |> PayrollElem.map List.average

//     //let inline max (xs: PayrollElem< ^a list> when ^a: comparison): PayrollElem< ^a > = xs |> PayrollElem.map List.max

//     let inline min (xs: PayrollElem< ^a list> when ^a: comparison): PayrollElem< ^a > = xs |> PayrollElem.map List.min

//     let all = PayrollElem.map (List.reduce (&&))

//     let any = PayrollElem.map (List.reduce (||))