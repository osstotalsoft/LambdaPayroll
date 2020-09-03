namespace LambdaPayroll.Application

open NBB.Application.Mediator.FSharp
open NBB.Core.Effects.FSharp

open System

module Middleware = 

    let log =
        fun next req ->
            effect {
                let reqType = req.GetType().FullName
                printfn "Precessing %s" reqType
                let! result = next req
                printfn "Precessed %s" reqType
                return result
            }

    let publishMessage =
        fun req ->
            effect {
                do! MessageBus.publish req
                return Some()
            }

module ReadApplication =
    open LambdaPayroll.Application.Evaluation
    open RequestMiddleware
    open QueryHandler
    open Middleware

    let private queryPipeline =
        log
        << handlers [ EvaluateSingleCode.handle |> upCast
                      EvaluateMultipleCodes.handle |> upCast
                      Compilation.GetGeneratedCode.handle |> upCast ]

    let private commandPipeline = log << lift publishMessage

    let sendQuery (query: 'TQuery) = 
        QueryMidleware.run queryPipeline query

    let sendCommand (cmd: 'TCommand) =
        CommandMiddleware.run commandPipeline cmd


module WriteApplication =
    open RequestMiddleware
    open CommandHandler
    open LambdaPayroll.Application.Compilation
    open Middleware

    let private commandPipeline =
        log
        << handlers [ AddDbElemDefinition.handle |> upCast
                      lift AddFormulaElemDefinition.validate AddFormulaElemDefinition.handle |> upCast 
                      Compile.handle |> upCast]

    open EventMiddleware
    open EventHandler

    let private eventPipeline: EventMiddleware =
        log
        << handlers [
            ElemDefinitionAdded.handle |> upCast 
        ]


    let sendCommand (cmd: 'TCommand) = CommandMiddleware.run commandPipeline cmd 
    let publishEvent (ev: 'TEvent) = EventMiddleware.run eventPipeline ev 
