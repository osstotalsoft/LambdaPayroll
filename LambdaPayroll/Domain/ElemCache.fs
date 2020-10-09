module ElemCache

open ElemAlgebra
open NBB.Core.Effects.FSharp
open System.Collections.Generic

//type GetSideEffect<'a> = {
//    Elem: PayrollElem<'a>
//    ContractId: ContractId
//    YearMonth: YearMonth
//}
//with interface ISideEffect<Result<'a, string> option>

//type SetSideEffect<'a> = {
//    Elem: PayrollElem<'a>
//    ContractId: ContractId
//    YearMonth: YearMonth
//    Value: Result<'a, string>
//}
//with interface ISideEffect<unit>

type CacheKey = {
    ElemCode: int
    Context: PayrollElemContext
}
module CacheKey =
    let create elem (ctx: PayrollElemContext) = 
        let boxed = box elem
        let elemCode = boxed.GetHashCode ()
        {ElemCode=elemCode; Context = ctx}

let private cache = new Dictionary<CacheKey, obj>()

let get (elem:PayrollElem<'a>) (ctx: PayrollElemContext): Effect<Result<'a, string> option> = 
    effect {
        let key = CacheKey.create elem ctx
        let found, value = cache.TryGetValue key
        if found 
        then return Some (value:?> Result<'a, string>)
        else return None
    }

let set (elem:PayrollElem<'a>) (ctx: PayrollElemContext) (value:Result<'a, string>): Effect<unit> = 
    effect {
        let key = CacheKey.create  elem ctx
        cache.Add(key, value:> obj)
    }
    

