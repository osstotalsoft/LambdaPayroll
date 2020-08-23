namespace LambdaPayroll.Infra

open LambdaPayroll.Domain

module GeneratedCodeCache =
    let mutable private cache: string option = None

    let get (_: GeneratedCodeCache.GetGeneratedCodeSideEffect)  =
        match cache with
        | Some(sourceCode) -> Ok sourceCode
        | None -> Error "Generated source code not available"

    let set (GeneratedCodeCache.SetGeneratedCodeSideEffect sourceCode) =
        cache <- Some sourceCode

    