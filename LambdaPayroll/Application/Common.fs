namespace LambdaPayroll.Application

open NBB.Messaging.Effects
open NBB.Core.Effects.FSharp

// TODO: Find a place for MesageBus wrapper
module MessageBus =
    let publish (obj: 'TMessage) =  MessageBus.Publish (obj :> obj) |> Effect.wrap |> Effect.ignore
    
//TODO implement a mediator like solution for events
module Mediator = 
    let dispatchEvent (_event:'e) = Effect.pure' ()
    let dispatchEvents (events: 'e list) = List.traverseEffect dispatchEvent events |> Effect.ignore

