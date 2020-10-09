// Learn more about F# at http://fsharp.org
open ElemAlgebra
open SamplePayrollScheme
open NBB.Core.Effects.FSharp
open System

let mainEff _argv = 
    effect {
        let ctx = ((ContractId 1), (YearMonth (2020, 7)))
        let! nowOnce = eval now ctx
        printfn "now = %A" nowOnce

        let! impoziteleNerotunjitePeToateContractele = eval impoziteleNerotunjitePeToateContractele ctx
        printfn "impoziteleNerotunjitePeToateContractele = %A" impoziteleNerotunjitePeToateContractele

        let! impozitelePeToateContractele = eval impozitelePeToateContractele ctx
        printfn "impozitelePeToateContractele = %A" impozitelePeToateContractele

        let! sumaImpozitelorPeToateContractele = eval sumaImpozitelorPeToateContractele ctx
        printfn "sumaImpozitelorPeToateContractele = %A" sumaImpozitelorPeToateContractele

        let! salariuNet = eval salariuNet ctx
        printfn "salariuNet = %A" salariuNet

        let! diferentaNetFataDeLunaTrecuta = eval diferentaNetFataDeLunaTrecuta ctx
        printfn "diferentaNetFataDeLunaTrecuta = %A" diferentaNetFataDeLunaTrecuta

        let! mediaSalariuluiNetPeUltimele3Luni = eval mediaSalariuluiNetPeUltimele3Luni ctx
        printfn "mediaSalariuluiNetPeUltimele3Luni = %A" mediaSalariuluiNetPeUltimele3Luni

        let! esteContractPrincipalSiNuEsteActivAcum2Luni = eval esteContractPrincipalSiNuEsteActivAcum2Luni ctx
        printfn "esteContractPrincipalSiNuEsteActivAcum2Luni = %A" esteContractPrincipalSiNuEsteActivAcum2Luni

        let! esteContractPrincipalSiAreToateContracteleActive = eval esteContractPrincipalSiAreToateContracteleActive ctx
        printfn "esteContractPrincipalSiAreToateContracteleActive = %A" esteContractPrincipalSiAreToateContracteleActive

        let! esteContractPrincipalSiAreVreunContractInactivLunaTrecuta = eval esteContractPrincipalSiAreVreunContractInactivLunaTrecuta ctx
        printfn "esteContractPrincipalSiAreVreunContractInactivLunaTrecuta = %A" esteContractPrincipalSiAreVreunContractInactivLunaTrecuta

        let! ultimele3Luni = eval ultimele3Luni ctx
        printfn "ultimele3Luni = %A" ultimele3Luni
        
        let! nowAgain = eval now ctx
        printfn "now = %A" nowAgain

        Console.ReadKey() |> ignore

        return 0
    }

[<EntryPoint>]
let main argv =
    let interpreter = Interpreter.createInterpreter ()
    mainEff argv
        |> Effect.interpret interpreter
        |> Async.RunSynchronously

