namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open HandlerUtils
open Giraffe
open LambdaPayroll.Application
open LambdaPayroll.Application.Compilation
open Application

module Compilation =
    let handler : HttpHandler = 
        subRoute "/compilation" (
            choose [
                GET >=> route  "/getGeneratedCode"  >=> ((ReadApplication.sendQuery (GetGeneratedCode.Query())) |> interpret textResultOption)
            ])