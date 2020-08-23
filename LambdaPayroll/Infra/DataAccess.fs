namespace LambdaPayroll.Infra

open System
open FSharp.Data
open LambdaPayroll.Domain

module DataAccess =

    module ElemDefinitionStoreRepo =
        let mutable private cache: ElemDefinitionStore option = None

        type SelectElemDefinitionsCommand = SqlCommandProvider<"SELECT * FROM VW_ElemDefinitions" , "name=LambdaPayroll">
        type LambdaPayrollDb = SqlProgrammabilityProvider<"name=LambdaPayroll">

        let private splitDeps : string option -> string list = 
            function
            | Some deps -> deps.Split(';') |> Array.toList
            | None -> []

        let loadCurrent (connectionString: string) (_: ElemDefinitionStoreRepo.LoadCurrentElemDefinitionStoreSideEffect)  =
            match cache with
            | Some(store) -> store
            | None ->
                use cmd = new SelectElemDefinitionsCommand(connectionString)

                let store = 
                    let results = cmd.Execute ()
                    in results |> Seq.map (
                        fun item  -> 
                            let elemCode = ElemCode(item.Code)
                            in {
                                Code = elemCode
                                Type = 
                                    match item.Type with
                                    | Some "Formula" -> Formula {Formula = item.Formula.Value; Deps= (splitDeps item.FormulaDeps) }
                                    | Some "Db" -> Db { TableName = item.TableName.Value; ColumnName = item.ColumnName.Value}
                                    | _ -> failwith "DB configuration errror"
                                DataType = Type.GetType(item.DataType)
                            }
                        )
                    |> ElemDefinitionStore.create

                cache <- Some store
                store

        let save (connectionString: string) (sideEffect: ElemDefinitionStoreRepo.SaveElemDefinitionStoreSideEffect) =
            use conn = new System.Data.SqlClient.SqlConnection (connectionString)
            conn.Open()

            let insertElemDefinition elemDefinition = 
                let (ElemCode code) = elemDefinition.Code
                let dataType = elemDefinition.DataType.FullName
                let elemDefinitions = new LambdaPayrollDb.dbo.Tables.ElemDefinition()
                let newRow = elemDefinitions.NewRow(code, dataType)
                elemDefinitions.Rows.Add newRow
                elemDefinitions.Update(conn) |> ignore
                newRow.ElemDefinitionId

            let insertDbElemDefinition (dbElemDefinition: DbElemDefinition) elemDefinitionId =
                let dbElemDefinitions = new LambdaPayrollDb.dbo.Tables.DbElemDefinition()
                dbElemDefinitions.AddRow(dbElemDefinition.TableName, dbElemDefinition.ColumnName, elemDefinitionId)
                dbElemDefinitions.Update(conn) |> ignore
              
            let insertFormulaElemDefinition (formulaElemDefinition: FormulaElemDefinition) elemDefinitionId =
                let formulaElemDefinitions = new LambdaPayrollDb.dbo.Tables.FromulaElemDefinition()
                formulaElemDefinitions.AddRow(formulaElemDefinition.Formula, elemDefinitionId)
                formulaElemDefinitions.Update(conn) |> ignore

            let processEvent event = 
                match event with
                | ElemDefinitionAdded (_elemDefinitionStoreId, elemDefinition) ->
                    let elemDefinitionId = insertElemDefinition elemDefinition
                    match elemDefinition.Type with
                        |Db dbElemDefinition -> insertDbElemDefinition dbElemDefinition elemDefinitionId
                        |Formula formulaElemDefinition -> insertFormulaElemDefinition formulaElemDefinition elemDefinitionId
                | ElemDefinitionStoreCreated _ -> ()

            let (ElemDefinitionStoreRepo.SaveElemDefinitionStoreSideEffect (_store, events)) = sideEffect
            events |> List.map processEvent |> ignore
            cache <- None

    module DbElemValue =
        open System.Data.SqlClient
        open Core

        module SqlCommandHelper =
            let private exec connection bind (query: string) (parametres: (string * obj) list)  = 
                use conn = new SqlConnection (connection)
                conn.Open()
                use cmd = new SqlCommand (query, conn)
                do parametres |> List.iter (fun (key, value) -> cmd.Parameters.AddWithValue(key, value) |> ignore) 
                bind cmd

            let execute connection = exec connection <| fun c -> c.ExecuteNonQuery() |> ignore
            let executeScalar connection = exec connection <| fun c -> c.ExecuteScalar()

        let loadValue (connectionString: string) ({Definition=definition; Context=context} : HrAdmin.LoadSideEffect) : Result<obj, string> =
            let executeCommand  = SqlCommandHelper.executeScalar connectionString   
            let {TableName=table; ColumnName=column} = definition
            let (ContractId contractId), (YearMonth (year, month)) = context
            let result = 
                executeCommand
                    (sprintf "SELECT TOP 1 %s FROM %s WHERE ContractId=@ContractId AND Month=@Month AND Year=@Year" column table)
                    ["@ContractId", box contractId; "@Month", box month; "@Year", box year] 

            Result.Ok result 
