namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open HandlerUtils
open Giraffe
open LambdaPayroll.Application.Evaluation

module Evaluation =
    let handler : HttpHandler = 
        subRoute "/evaluation" (
            choose [
                POST >=> route  "/evaluateSingleCode"  >=> bindJson<EvaluateSingleCode.Query> (EvaluateSingleCode.handler >> (interpret jsonResult))
                POST >=> route  "/evaluateMultipleCodes"  >=> bindJson<EvaluateMultipleCodes.Query> (EvaluateMultipleCodes.handler >> (interpret jsonResult))
            ])