namespace LambdaPayroll.Application.InfraEffects

open LambdaPayroll.Domain
open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open Core

module CodeGenerationService =
    type GenerateSourceCodeSideEffect =
        | GenerateSourceCodeSideEffect of definitionStore: ElemDefinitionStore
        interface ISideEffect<Result<string, string>>

    let generateSourceCode elemDefinitionStore =
        Effect.Of(GenerateSourceCodeSideEffect elemDefinitionStore)
        |> Effect.wrap

    type GenerateExpresionSideEffect =
        | GenerateExpressionSideEffect of formula: string
        interface ISideEffect<Result<string, string>>

    let generatExpression formula =
        Effect.Of(GenerateExpressionSideEffect formula)
        |> Effect.wrap


module GeneratedCodeCache =
    type GetGeneratedCodeSideEffect() =
        interface ISideEffect<Result<string, string>>

    type SetGeneratedCodeSideEffect =
        | SetGeneratedCodeSideEffect of sourceCode: string
        interface ISideEffect<unit>

    let get =
        Effect.Of(GetGeneratedCodeSideEffect())
        |> Effect.wrap

    let set sourceCode =
        Effect.Of(SetGeneratedCodeSideEffect sourceCode)
        |> Effect.wrap