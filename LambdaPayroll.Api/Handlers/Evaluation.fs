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
                POST >=> route  "/evaluateSingleCode"  >=> bindJson<EvaluateSingleCode.Query> (ReadApplication.sendQuery >> (interpret jsonResultOption))
                POST >=> route  "/evaluateMultipleCodes"  >=> bindJson<EvaluateMultipleCodes.Query> (ReadApplication.sendQuery >> (interpret jsonResultOption))
                POST >=> route  "/evaluateExpression"  >=> bindJson<EvaluateExpression.Query> (ReadApplication.sendQuery >> (interpret jsonResultOption))
            ])