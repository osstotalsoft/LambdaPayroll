namespace LambdaPayroll.Infra

open LambdaPayroll.Domain.Exception

module Common = 

    let handleException (ExceptionSideEffect msg) = failwith msg |> ignore
        
