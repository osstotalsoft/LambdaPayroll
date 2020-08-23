namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open LambdaPayroll.PublishedLanguage
open HandlerUtils
open Giraffe

module ElemDefinitions =
    let handler : HttpHandler = 
        subRoute "/elemDefinitions" (
            choose [
                POST >=> route  "/addDbElem"  >=> bindJson<AddDbElemDefinition> publishCommand
                POST >=> route  "/addFormulaElem"  >=> bindJson<AddFormulaElemDefinition> publishCommand
            ])