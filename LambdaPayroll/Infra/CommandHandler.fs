namespace LambdaPayroll.Infra

open NBB.Core.Abstractions
open System
open NBB.Core.Effects.FSharp

module CommandHandler =
    type HandlerFunc<'TCommand when 'TCommand:> ICommand> = ('TCommand -> Effect<unit>)
    type CommandHandler  = (ICommand -> Effect<unit>)
    type HandlerRegistration = (Type * CommandHandler)

    let private wrap<'TCommand when 'TCommand:> ICommand> (handlerFunc : HandlerFunc<'TCommand>) : CommandHandler= 
        fun sideEffect ->
            match sideEffect with
                | :? 'TCommand as command -> handlerFunc(command) //|> Task.FromResult
                | _ -> failwith "Wrong type"


    let createCommandHandler(handlerRegistrations: seq<HandlerRegistration>) : CommandHandler =
        let handlersMap = handlerRegistrations |> Seq.map (fun (key, value) -> (key.FullName, value)) |> Map.ofSeq

        fun (sideEffect) ->
            let handlerOption = handlersMap |> Map.tryFind (sideEffect.GetType().FullName)
            match handlerOption with
            | Some handler -> handler sideEffect
            | _ -> failwith "Invalid handler"

    let toCommandHandlerReg (func: HandlerFunc<'TCommand>) : HandlerRegistration =
        (typeof<'TCommand>,  wrap(func))      
