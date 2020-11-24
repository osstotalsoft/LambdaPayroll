namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp

module Exception = 

    type ExceptionSideEffect = ExceptionSideEffect of string
    with interface ISideEffect<unit>

    let throw = Effect.Of << ExceptionSideEffect

[<AutoOpen>]
module ListExtensions =
    [<RequireQualifiedAccess>]
    module List =
        let flatten (listOfLists: list<list<'a>>): list<'a> = listOfLists |> List.collect id