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
open NBB.Messaging.Nats
open NBB.Core.Effects
open NBB.Correlation.AspNet
open LambdaPayroll.Infra
open DynamicAssembly
open LambdaPayroll.Infra.DataAccess

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

    let configureServices (context: WebHostBuilderContext) (services : IServiceCollection) =
        let payrollConnString = context.Configuration.GetConnectionString "LambdaPayroll"
        let hcmConnectionString = context.Configuration.GetConnectionString "Hcm"

        services.AddHostedService<HostedServices.CompileDefinitions>() |> ignore
        services.AddEffects() |> ignore
        services.AddMessagingEffects() |> ignore
        services
            //.AddSideEffectHandler(FormulaParser.parse)
            .AddSideEffectHandler(DynamicAssemblyCache.get)
            .AddSideEffectHandler(DynamicAssemblyCache.set)
            .AddSideEffectHandler(GeneratedCodeCache.get)
            .AddSideEffectHandler(GeneratedCodeCache.set)
            .AddSideEffectHandler(DynamicAssembly.compile)
            .AddSideEffectHandler(ElemDefinitionStoreRepo.loadCurrent payrollConnString)
            .AddSideEffectHandler(DbElemValue.loadValue hcmConnectionString)
            |> ignore;

        services.AddNatsMessaging() |> ignore
        services.AddCors()
            .AddGiraffe() 
            .AddSingleton<IJsonSerializer>(
                NewtonsoftJsonSerializer(NewtonsoftJsonSerializer.DefaultSettings))
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