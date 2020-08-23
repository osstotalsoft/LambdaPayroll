namespace LambdaPayroll.Domain

open NBB.Core.Effects
open NBB.Core.Effects.FSharp

module Exception = 

    type ExceptionSideEffect = ExceptionSideEffect of string
    with interface ISideEffect<unit>

    let throw = Effect.wrap << Effect.Of << ExceptionSideEffect

