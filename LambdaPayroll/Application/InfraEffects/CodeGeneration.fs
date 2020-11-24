namespace LambdaPayroll.Application.InfraEffects

open LambdaPayroll.Domain
open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open ElemAlgebra

module CodeGenerationService =
    type GenerateSourceCodeSideEffect =
        | GenerateSourceCodeSideEffect of definitionStore: ElemDefinitionStore
        interface ISideEffect<Result<string, string>>

    let generateSourceCode elemDefinitionStore =
        Effect.Of(GenerateSourceCodeSideEffect elemDefinitionStore)

    type GenerateExpresionSideEffect =
        | GenerateExpressionSideEffect of formula: string
        interface ISideEffect<Result<string, string>>

    let generatExpression formula =
        Effect.Of(GenerateExpressionSideEffect formula)


module GeneratedCodeCache =
    type GetGeneratedCodeSideEffect() =
        interface ISideEffect<Result<string, string>>

    type SetGeneratedCodeSideEffect =
        | SetGeneratedCodeSideEffect of sourceCode: string
        interface ISideEffect<unit>

    let get =
        Effect.Of(GetGeneratedCodeSideEffect())

    let set sourceCode =
        Effect.Of(SetGeneratedCodeSideEffect sourceCode)