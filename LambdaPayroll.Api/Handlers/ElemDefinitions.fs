namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open LambdaPayroll.PublishedLanguage
open LambdaPayroll.Application
open HandlerUtils
open Giraffe

module ElemDefinitions =
   
    let handler : HttpHandler = 
        subRoute "/elemDefinitions" (
            choose [
                POST >=> route  "/addFormulaElem"  >=> bindJson<AddFormulaElemDefinition> (Mediator.sendCommand |> interpretCommand)
                POST >=> route  "/addDbElem"  >=> bindJson<AddDbElemDefinition> (Mediator.sendCommand |> interpretCommand)
            ])