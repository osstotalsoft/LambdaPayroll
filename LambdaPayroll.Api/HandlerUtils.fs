﻿namespace LambdaPayroll.Api

open NBB.Core.Abstractions
open NBB.Application.DataContracts
open NBB.Correlation

// TODO: Find a place for MesageBus wrapper
module MessageBus =
    open NBB.Core.Effects.FSharp
    open NBB.Messaging.Effects

    let publish (obj: 'TMessage) =  MessageBus.Publish (obj :> obj) |> Effect.wrap |> Effect.ignore

module HandlerUtils =
    open Giraffe
    open NBB.Core.Effects
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2
    open Microsoft.Extensions.DependencyInjection

    type Effect<'a> = FSharp.Effect<'a>
    module Effect = FSharp.Effect

    let setError errorText = 
        (clearResponse >=> setStatusCode 500 >=> text errorText)

    let interpret<'TResult> (resultHandler: 'TResult -> HttpHandler) (effect: Effect<'TResult>) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let interpreter = ctx.RequestServices.GetRequiredService<IInterpreter>()
                let! result = interpreter.Interpret(effect |> Effect.unWrap)
                return! (result |> resultHandler) next ctx
            }   

    let jsonResult = function
        | Ok value -> json value
        | Error err -> setError err

    let textResult = function
        | Ok value -> text value
        | Error err -> setError err

    let commandResult (command : IMetadataProvider<CommandMetadata>) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let result = {| 
                CommandId = command.Metadata.CommandId 
                CorrelationId = CorrelationManager.GetCorrelationId() 
            |}

            Successful.OK result next ctx

    let interpretCommand handler command = 
        let resultHandler _ = commandResult command
        command |> handler |> interpret resultHandler

    let publishCommand : (IMetadataProvider<CommandMetadata> -> HttpHandler) = 
        MessageBus.publish |> interpretCommand 
        