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

module PipelineUtils = 
    let terminateRequest<'a> : (Effect<'a option> -> Effect<'a>) =
        Effect.map (function
            | Some value -> value
            | None -> failwith "No handler found")

    let terminateEvent : (Effect<unit option> -> Effect<unit>) =
        Effect.map ignore


module ReadApplication =
    open LambdaPayroll.Application.Evaluation
    open NBB.Core.Abstractions
    open RequestMiddleware
    open QueryHandler
    open Middleware
    open PipelineUtils

    let private queryPipeline =
        log
        << handlers [ EvaluateSingleCode.handle |> upCast
                      EvaluateMultipleCodes.handle |> upCast
                      EvaluateExpression.handle |> upCast
                      Compilation.GetGeneratedCode.handle |> upCast ]

    let private commandPipeline = log << lift publishMessage

    let sendQuery (query: 'TQuery) = 
        QueryMidleware.run queryPipeline query |> terminateRequest
    
    let sendQuery' (query: IQuery) = 
        RequestMiddleware.run queryPipeline query |> terminateRequest

    let sendCommand (cmd: 'TCommand) =
        CommandMiddleware.run commandPipeline cmd |> terminateRequest

    let publishEvent (ev: 'TEvent) = EventHandler.empty ev |> terminateEvent

module WriteApplication =
    open RequestMiddleware
    open CommandHandler
    open LambdaPayroll.Application.Compilation
    open NBB.Core.Abstractions
    open Middleware
    open PipelineUtils

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


    let sendCommand (cmd: 'TCommand) = CommandMiddleware.run commandPipeline cmd |> terminateRequest
    let publishEvent (ev: 'TEvent) = EventMiddleware.run eventPipeline ev |> terminateEvent
    let sendQuery' (q: IQuery) = RequestHandler.empty q |> terminateRequest
