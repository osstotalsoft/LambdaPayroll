// Learn more about F# at http://fsharp.org
open Core
open SamplePayrollScheme
open NBB.Core.Effects.FSharp

let mainEff _argv = 
    effect {
        let! nowOnce = eval now (ContractId 1) (YearMonth (2020, 7))
        printfn "now = %A" nowOnce

        let! impoziteleNerotunjitePeToateContractele = eval impoziteleNerotunjitePeToateContractele (ContractId 1) (YearMonth (2020, 7))
        printfn "impoziteleNerotunjitePeToateContractele = %A" impoziteleNerotunjitePeToateContractele

        let! impozitelePeToateContractele = eval impozitelePeToateContractele (ContractId 1) (YearMonth (2020, 7))
        printfn "impozitelePeToateContractele = %A" impozitelePeToateContractele

        let! sumaImpozitelorPeToateContractele = eval sumaImpozitelorPeToateContractele (ContractId 1) (YearMonth (2020, 7))
        printfn "sumaImpozitelorPeToateContractele = %A" sumaImpozitelorPeToateContractele

        let! salariuNet = eval salariuNet (ContractId 1) (YearMonth (2020, 7))
        printfn "salariuNet = %A" salariuNet

        let! diferentaNetFataDeLunaTrecuta = eval diferentaNetFataDeLunaTrecuta (ContractId 1) (YearMonth (2020, 7))
        printfn "diferentaNetFataDeLunaTrecuta = %A" diferentaNetFataDeLunaTrecuta

        let! mediaSalariuluiNetPeUltimele3Luni = eval mediaSalariuluiNetPeUltimele3Luni (ContractId 1) (YearMonth (2020, 7))
        printfn "mediaSalariuluiNetPeUltimele3Luni = %A" mediaSalariuluiNetPeUltimele3Luni

        let! esteContractPrincipalSiNuEsteActivAcum2Luni = eval esteContractPrincipalSiNuEsteActivAcum2Luni (ContractId 1) (YearMonth (2020, 7))
        printfn "esteContractPrincipalSiNuEsteActivAcum2Luni = %A" esteContractPrincipalSiNuEsteActivAcum2Luni

        let! esteContractPrincipalSiAreToateContracteleActive = eval esteContractPrincipalSiAreToateContracteleActive (ContractId 1) (YearMonth (2020, 7))
        printfn "esteContractPrincipalSiAreToateContracteleActive = %A" esteContractPrincipalSiAreToateContracteleActive

        let! esteContractPrincipalSiAreVreunContractInactivLunaTrecuta = eval esteContractPrincipalSiAreVreunContractInactivLunaTrecuta (ContractId 1) (YearMonth (2020, 7))
        printfn "esteContractPrincipalSiAreVreunContractInactivLunaTrecuta = %A" esteContractPrincipalSiAreVreunContractInactivLunaTrecuta

        let! ultimele3Luni = eval ultimele3Luni (ContractId 1) (YearMonth (2020, 7))
        printfn "ultimele3Luni = %A" ultimele3Luni
        
        let! nowAgain = eval now (ContractId 1) (YearMonth (2020, 7))
        printfn "now = %A" nowAgain


        return 0
    }

[<EntryPoint>]
let main argv =
    let interpreter = Interpreter.createInterpreter ()
    mainEff argv
        |> Effect.interpret interpreter
        |> Async.RunSynchronously


