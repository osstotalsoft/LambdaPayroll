module ElemCache

open Core
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
    ContractId: ContractId
    YearMonth: YearMonth
}
module CacheKey =
    let create elem contractId yearMonth = 
        let boxed = box elem
        let elemCode = boxed.GetHashCode ()
        {ElemCode=elemCode; ContractId=contractId; YearMonth=yearMonth}

let private cache = new Dictionary<CacheKey, obj>()

let get (elem:PayrollElem<'a>) contractId yearMonth: Effect<Result<'a, string> option> = 
    effect {
        let key = CacheKey.create  elem contractId yearMonth
        let found, value = cache.TryGetValue key
        if found 
        then return Some (value:?> Result<'a, string>)
        else return None
    }

let set (elem:PayrollElem<'a>) contractId yearMonth (value:Result<'a, string>): Effect<unit> = 
    effect {
        let key = CacheKey.create  elem contractId yearMonth
        cache.Add(key, value:> obj)
    }
    

