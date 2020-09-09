namespace LambdaPayroll.Application

open NBB.Messaging.Effects
open NBB.Core.Effects.FSharp
open NBB.Core.Effects
open NBB.Core.Abstractions

// TODO: Find a place for MesageBus wrapper
module MessageBus =
    let publish (obj: 'TMessage) =
        MessageBus.Publish(obj :> obj)
        |> Effect.wrap
        |> Effect.ignore

type Mediator =
    { DispatchEvent: IEvent -> Effect<unit>
      SendCommand: ICommand -> Effect<unit>
      SendQuery: IQuery -> Effect<obj> }

module Mediator =
    type GetMediatorSideEffect =
        | GetMediatorSideEffect
        interface ISideEffect<Mediator>

    let private getMediator =
        Effect.Of(GetMediatorSideEffect) |> Effect.wrap

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
            mediator.SendQuery(query :> IQuery)
            |> Effect.map unbox<'TResponse>)

    let handleGetMediator (m: Mediator) (_: GetMediatorSideEffect) = m
