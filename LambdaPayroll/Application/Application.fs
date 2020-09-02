namespace LambdaPayroll.Application
open NBB.Application.Mediator.FSharp
open NBB.Core.Effects.FSharp

open System

module Application = 
    
    let logRequest =
        fun next req ->
        effect {
            let reqType = req.GetType().FullName
            printfn "Precessing request of type %s" reqType
            let! result = next req
            printfn "Precessed request of type %s" reqType
            return result
        }

    let publishMessage = 
        fun req ->
            effect {
                do! MessageBus.publish req
                return Some ()
            }

    module ReadApplication = 
        open LambdaPayroll.Application.Evaluation
        open RequestMiddleware
        open QueryHandler

        let private queryPipeline = 
            logRequest 
            << handlers [
                //EvaluateSingleCode.handler |> upCast;
                //EvaluateMultipleCodes.handler |> upCast;
                //Compilation.GetGeneratedCode.handler |> upCast
            ]

        let private commandPipeline = 
            logRequest
            << lift publishMessage

        let sendQuery (query: 'TQuery) = QueryMidleware.run queryPipeline query
        let sendCommand (cmd: 'TCommand) = CommandMiddleware.run commandPipeline cmd


    module WriteApplication = 
        open RequestMiddleware
        open CommandHandler

        let private commandPipeline = 
            logRequest
            << handlers [
                AddDbElemDefinition.handler |> upCast;
                lift AddFormulaElemDefinition.validate AddFormulaElemDefinition.handle |> upCast;
            ]

        let sendCommand (cmd: 'TCommand) = CommandMiddleware.run commandPipeline cmd