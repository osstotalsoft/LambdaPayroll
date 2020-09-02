namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open LambdaPayroll.PublishedLanguage
open LambdaPayroll.Application
open HandlerUtils
open Giraffe
open Application

module ElemDefinitions =
   
    let handler : HttpHandler = 
        subRoute "/elemDefinitions" (
            choose [
                // POST >=> route  "/addFormulaElem"  >=> bindJson<AddFormulaElemDefinition> (ReadApplication.sendCommand |> interpretCommand) 
                POST >=> route  "/addFormulaElem"  >=> bindJson<AddFormulaElemDefinition> (WriteApplication.sendCommand |> interpretCommand)
                // POST >=> route  "/addDbElem"  >=> bindJson<AddDbElemDefinition> (ReadApplication.sendCommand |> interpretCommand)
                POST >=> route  "/addDbElem"  >=> bindJson<AddDbElemDefinition> (WriteApplication.sendCommand  |> interpretCommand)
            ])