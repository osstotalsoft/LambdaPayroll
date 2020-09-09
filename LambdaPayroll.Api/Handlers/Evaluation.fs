namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open HandlerUtils
open Giraffe
open LambdaPayroll.Application
open LambdaPayroll.Application.Evaluation

module Evaluation =
    let handler : HttpHandler = 
        subRoute "/evaluation" (
            choose [
                POST >=> route  "/evaluateSingleCode"  >=> bindJson<EvaluateSingleCode.Query> (Mediator.sendQuery >> (interpret jsonResult))
                POST >=> route  "/evaluateMultipleCodes"  >=> bindJson<EvaluateMultipleCodes.Query> (Mediator.sendQuery >> (interpret jsonResult))
                POST >=> route  "/evaluateExpression"  >=> bindJson<EvaluateExpression.Query> (Mediator.sendQuery >> (interpret jsonResult))
            ])