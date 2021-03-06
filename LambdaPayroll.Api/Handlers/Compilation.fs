﻿namespace LambdaPayroll.Api.Handlers

open LambdaPayroll.Api
open HandlerUtils
open Giraffe
open LambdaPayroll.Application
open LambdaPayroll.Application.Compilation

module Compilation =
    let handler : HttpHandler = 
        subRoute "/compilation" (
            choose [
                GET >=> route  "/getGeneratedCode"  >=> ((Mediator.sendQuery (GetGeneratedCode.Query())) |> interpret textResult)
            ])