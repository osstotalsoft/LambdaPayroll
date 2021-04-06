module Mediator

open LambdaPayroll.Application.Mediator
open LambdaPayroll.Application

let getReadApplicationMediator (_: GetMediatorSideEffect) =
    { new IMediator with
        member _.DispatchEvent(ev) = WriteApplication.publishEvent ev // temporary handle events in API
        member _.SendCommand(cmd) = WriteApplication.sendCommand cmd  // temporary handle commands in API
        member _.SendQuery(q) = ReadApplication.sendQuery q
    }

let getWriteApplicationMediator (_: GetMediatorSideEffect) =
    { new IMediator with
        member _.DispatchEvent(ev) = WriteApplication.publishEvent ev
        member _.SendCommand(cmd) = WriteApplication.sendCommand cmd
        member _.SendQuery(q) = WriteApplication.sendQuery q
    }