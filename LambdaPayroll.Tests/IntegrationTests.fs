module IntegrationTests

open System
open System.IO
open FsUnit.Xunit
open Microsoft.Extensions.Configuration
open NBB.Core.Effects
open NBB.Core.Effects.FSharp
open Xunit
open DbUp
open LambdaPayroll.Infra
open DataAccess
open LambdaPayroll.Application.Evaluation
open LambdaPayroll.Application.Compilation
open LambdaPayroll.Application
open LambdaPayroll.Migrations
open Microsoft.Extensions.DependencyInjection
open LambdaPayroll.Infra.DynamicAssembly

let configuration =
    let configurationBuilder = 
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
    configurationBuilder.Build()

let payrollConnString = configuration.GetConnectionString "LambdaPayroll"
let hcmConnectionString = configuration.GetConnectionString "Hcm"


let services = ServiceCollection();
services.AddEffects() |> ignore
services
    .AddSideEffectHandler(ElemDefinitionStoreRepo.loadCurrent payrollConnString)
    .AddSideEffectHandler(DbElemValue.loadValue hcmConnectionString)
    .AddSideEffectHandler(DynamicAssemblyCache.get)
    .AddSideEffectHandler(DynamicAssemblyCache.set)
    .AddSideEffectHandler(GeneratedCodeCache.get)
    .AddSideEffectHandler(GeneratedCodeCache.set)
    .AddSideEffectHandler(DynamicAssembly.compile)
    |> ignore



[<Fact>]
let ``It shoud evaluate formula with params (integration)`` () =

    // Arrange
    Migrator.upgradeDatabase true payrollConnString
    let populatePayrollDb = 
        DeployChanges.To
            .SqlDatabase(payrollConnString, null) 
            .WithScript("PopulateData", 
                "SET IDENTITY_INSERT [dbo].[ElemDefinition] ON 
                GO
                INSERT [dbo].[ElemDefinition] ([ElemDefinitionId], [Code], [DataType]) VALUES (1, N'SalariuBrut', N'System.Decimal')
                GO
                INSERT [dbo].[ElemDefinition] ([ElemDefinitionId], [Code], [DataType]) VALUES (2, N'Impozit', N'System.Decimal')
                GO
                INSERT [dbo].[ElemDefinition] ([ElemDefinitionId], [Code], [DataType]) VALUES (3, N'SalariuNet', N'System.Decimal')
                GO
                SET IDENTITY_INSERT [dbo].[ElemDefinition] OFF
                GO
                SET IDENTITY_INSERT [dbo].[DbElemDefinition] ON 
                GO
                INSERT [dbo].[DbElemDefinition] ([DbElemDefinitionId], [TableName], [ColumnName], [ElemDefinitionId]) VALUES (1, N'Salarii   ', N'SalariuBrut      ', 1)
                GO
                SET IDENTITY_INSERT [dbo].[DbElemDefinition] OFF
                GO
                SET IDENTITY_INSERT [dbo].[FromulaElemDefinition] ON 
                GO
                INSERT [dbo].[FromulaElemDefinition] ([FormulaId], [Formula], [ElemDefinitionId]) VALUES (1, N'@SalariuBrut * (Payroll.constant 0.1m)', 2)
                GO
                INSERT [dbo].[FromulaElemDefinition] ([FormulaId], [Formula], [ElemDefinitionId]) VALUES (2, N'@SalariuBrut - @Impozit', 3)
                GO
                SET IDENTITY_INSERT [dbo].[FromulaElemDefinition] OFF
                GO
                SET IDENTITY_INSERT [dbo].[ElemDependency] ON 
                GO
                INSERT [dbo].[ElemDependency] ([ElemDependencyId], [ElemDefinitionId], [DependencyElemDefinitionId]) VALUES (1, 2, 1)
                GO
                INSERT [dbo].[ElemDependency] ([ElemDependencyId], [ElemDefinitionId], [DependencyElemDefinitionId]) VALUES (2, 3, 1)
                GO
                INSERT [dbo].[ElemDependency] ([ElemDependencyId], [ElemDefinitionId], [DependencyElemDefinitionId]) VALUES (3, 3, 2)
                GO
                SET IDENTITY_INSERT [dbo].[ElemDependency] OFF
                GO")    
            .LogToConsole()
            .Build()
            .PerformUpgrade()

    if (not populatePayrollDb.Successful) then raise populatePayrollDb.Error
    
    DropDatabase.For.SqlDatabase(hcmConnectionString);
    EnsureDatabase.For.SqlDatabase(hcmConnectionString);
    let createHcmDb = 
        DeployChanges.To
            .SqlDatabase(hcmConnectionString, null) 
            .WithScript("CreateObjects", 
                "CREATE TABLE [dbo].[Salarii](
            	    [ContractId] [int] NOT NULL,
            	    [Month] [tinyint] NOT NULL,
            	    [Year] [smallint] NOT NULL,
            	    [SalariuBrut] [decimal](18, 0) NOT NULL
                ) ON [PRIMARY]
                GO")
            .WithScript("PopulateData", 
                "INSERT [dbo].[Salarii] ([ContractId], [Month], [Year], [SalariuBrut]) VALUES (1, 1, 2009, CAST(1000 AS Decimal(18, 0)))
                GO")
            .LogToConsole()
            .Build()
            .PerformUpgrade()

    if (not createHcmDb.Successful) then raise createHcmDb.Error

    let query : EvaluateMultipleCodes.Query = 
        { ElemCodes = ["SalariuNet"; "Impozit"]; ContractId = 1; Year=2009; Month=1;}

    use container = services.BuildServiceProvider();
    let interpreter = container.GetRequiredService<IInterpreter>()

    let compileEff = WriteApplication.sendCommand <| Compile.Command ()
    compileEff |> Effect.interpret interpreter |> Async.RunSynchronously |> ignore 

    let eff = ReadApplication.sendQuery query

    // Act
    let result = eff |> Effect.interpret interpreter |> Async.RunSynchronously

    // Assert  
    result |> should equal (Some(Ok ([900m :> obj; 100m :> obj])) : Result<obj list, string> option)
    