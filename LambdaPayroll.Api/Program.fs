namespace LambdaPayroll.Api

open System
open System.Reflection
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open NBB.Messaging.Effects
open NBB.Messaging.Host
open NBB.Messaging.Nats
open NBB.Messaging.Host.MessagingPipeline;
open NBB.Resiliency
open NBB.Core.Effects
open NBB.Core.Abstractions
open NBB.Correlation.AspNet
open LambdaPayroll.Infra
open DynamicAssembly
open InteractiveEvalSession
open LambdaPayroll.Infra.DataAccess
open LambdaPayroll.Application
open LambdaPayroll.PublishedLanguage


// ---------------------------------
// Web app
// ---------------------------------

module App =
    let webApp =
        choose [
            route "/" >=>  text "Hello"
            subRoute "/api"
                (choose [
                    Handlers.Evaluation.handler
                    Handlers.ElemDefinitions.handler
                    Handlers.Compilation.handler
                ])
            setStatusCode 404 >=> text "Not Found" ]

    // ---------------------------------
    // Error handler
    // ---------------------------------

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message

    // ---------------------------------
    // Config and Main
    // ---------------------------------

    let configureCors (builder : CorsPolicyBuilder) =
        builder.WithOrigins("http://localhost:5000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               |> ignore

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
        (match env.IsDevelopment() with
        | true  -> app.UseDeveloperExceptionPage()
        | false -> app.UseGiraffeErrorHandler errorHandler)
            .UseCors(configureCors)
            .UseCorrelation()
            .UseGiraffe(webApp)

    open NBB.Core.Effects.FSharp
    let configureServices (context: WebHostBuilderContext) (services : IServiceCollection) =
        let payrollConnString = context.Configuration.GetConnectionString "LambdaPayroll"
        let hcmConnectionString = context.Configuration.GetConnectionString "Hcm"

        let mediator =
            { SendCommand = WriteApplication.sendCommand
              SendQuery = ReadApplication.sendQuery'
              DispatchEvent = WriteApplication.publishEvent }
        
        services.AddHostedService<HostedServices.CompileDefinitions>() |> ignore
        services.AddEffects() |> ignore
        services.AddMessagingEffects() |> ignore
        services
            .AddSideEffectHandler(Common.handleException)
            .AddSideEffectHandler(DynamicAssemblyCache.get)
            .AddSideEffectHandler(DynamicAssemblyCache.set)
            .AddSideEffectHandler(GeneratedCodeCache.get)
            .AddSideEffectHandler(GeneratedCodeCache.set)
            .AddSideEffectHandler(DynamicAssemblyService.compile)
            .AddSideEffectHandler(ElemDefinitionStoreRepo.loadCurrent payrollConnString)
            .AddSideEffectHandler(DbElemValue.loadValue hcmConnectionString)
            .AddSideEffectHandler(DbElemValue.getAllEmployeeContracts hcmConnectionString)
            .AddSideEffectHandler(DbElemValue.getOtherEmployeeContracts hcmConnectionString)
            .AddSideEffectHandler(InteractiveEvalSessionCache.get)
            .AddSideEffectHandler(InteractiveEvalSessionCache.set)
            .AddSideEffectHandler(InteractiveSession.createSession)
            .AddSideEffectHandler(InteractiveSession.evalToPayrollElem )
            .AddSideEffectHandler(Mediator.handleGetMediator mediator)
            .AddSideEffectHandler(CodeGenerationService.generateSourceCode)
            .AddSideEffectHandler(CodeGenerationService.generateExpression)
            .AddSideEffectHandler(DynamicAssemblyService.findPayrollElem)
            .AddSideEffectHandler(FormulaParsingService.getFormulaDeps)
            // To be used from the Worker process
            .AddSideEffectHandler(ElemDefinitionStoreRepo.save payrollConnString)
            |> ignore;

        services.AddNatsMessaging() |> ignore
        services.AddCors()
            .AddGiraffe() 
            .AddSingleton<IJsonSerializer>(
                NewtonsoftJsonSerializer(NewtonsoftJsonSerializer.DefaultSettings))
            |> ignore

        // To be used from the Worker process
        
        let applicationPipeline = 
            fun message ->
                match box message with
                | :? IEvent as event -> Mediator.dispatchEvent event
                | _ -> failwith "Invalid message"

        
        services.AddResiliency() |> ignore
        services
            .AddMessagingHost()
                .AddSubscriberServices(fun config -> config.AddTypes( typeof<ElemDefinitionAdded >) |> ignore)
                .WithDefaultOptions()
                .UsePipeline(fun pipelineBuilder -> 
                    pipelineBuilder
                        .UseCorrelationMiddleware()
                        .UseExceptionHandlingMiddleware()
                        .UseDefaultResiliencyMiddleware()
                        .UseEffectMiddleware(fun m -> m |> applicationPipeline |> Effect.unWrap |> EffectExtensions.ToUnit)
                        |> ignore
                )
            |> ignore


    let configureAppConfiguration  (context: WebHostBuilderContext) (config: IConfigurationBuilder) =  
        config
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName, true)
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .AddEnvironmentVariables() |> ignore

    let configureLogging (builder : ILoggingBuilder) =
        builder.AddFilter(fun l -> l.Equals LogLevel.Error)
               .AddConsole()
               .AddDebug() |> ignore

    [<EntryPoint>]
    let main _ =
        WebHostBuilder()
            .UseKestrel()
            .ConfigureAppConfiguration(configureAppConfiguration)
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging)
            .Build()
            .Run()
        0