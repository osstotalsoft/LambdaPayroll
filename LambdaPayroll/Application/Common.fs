namespace LambdaPayroll.Application

open NBB.Messaging.Effects
open NBB.Core.Effects.FSharp
open NBB.Core.Effects
open NBB.Core.Abstractions

// TODO: Find a place for MesageBus wrapper
module MessageBus =
    let publish (obj: 'TMessage) =
        MessageBus.Publish(obj :> obj)
        |> Effect.ignore

type IMediator = 
    abstract member DispatchEvent: IEvent -> Effect<unit>
    abstract member SendCommand: ICommand -> Effect<unit>
    abstract member SendQuery: IQuery<'a> -> Effect<'a>
          

module Mediator =
    type GetMediatorSideEffect =
        | GetMediatorSideEffect
        interface ISideEffect<IMediator>

    let private getMediator =
        Effect.Of(GetMediatorSideEffect)

    let dispatchEvent (event: #IEvent) =
        getMediator
        |> Effect.bind (fun mediator -> mediator.DispatchEvent(event :> IEvent))

    let sendCommand (cmd: #ICommand) =
        getMediator
        |> Effect.bind (fun mediator -> mediator.SendCommand(cmd :> ICommand))

    let dispatchEvents (events: #IEvent list) =
        List.traverseEffect dispatchEvent events
        |> Effect.ignore

    let sendQuery (query: #IQuery<'TResponse>) =
        getMediator
        |> Effect.bind (fun mediator ->
            mediator.SendQuery(query))

