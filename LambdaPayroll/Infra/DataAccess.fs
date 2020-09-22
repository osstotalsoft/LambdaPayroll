namespace LambdaPayroll.Infra

open System
open LambdaPayroll.Domain

module DataAccess =
    open System.Data.SqlClient
    open Core

    let unboxOption<'a> (o: obj): 'a option =
        if (isNull o) || DBNull.Value.Equals o then None else Some(unbox o)

    module SqlCommandHelper =
        let private exec connection bind (query: string) (parametres: (string * obj) list) =
            use conn = new SqlConnection(connection)
            conn.Open()
            use cmd = new SqlCommand(query, conn)
            do parametres
               |> List.iter (fun (key, value) -> cmd.Parameters.AddWithValue(key, value) |> ignore)
            bind cmd


        let execute connection =
            exec connection
            <| fun c -> c.ExecuteNonQuery() |> ignore

        let executeScalar connection =
            exec connection
            <| fun c -> c.ExecuteScalar()

        let read connection f =
            exec connection
            <| fun c ->
                [ let read = c.ExecuteReader()
                  while read.Read() do
                      yield f read ]

    module ElemDefinitionStoreRepo =
        open Newtonsoft.Json

        let mutable private cache: ElemDefinitionStore option = None

        let private splitDeps: string option -> string list =
            function
            | Some deps -> deps.Split(';') |> Array.toList
            | None -> []

        let loadCurrent (connectionString: string) (_: ElemDefinitionStoreRepo.LoadCurrentElemDefinitionStoreSideEffect) =
            let read = SqlCommandHelper.read connectionString

            match cache with
            | Some (store) -> store
            | None ->
                let store =
                    let results =
                        read (fun r ->
                            {| Code = unbox r.[0]
                               DataType = unbox r.[1]
                               Type = unbox r.[2]
                               TableName = unboxOption r.[3]
                               ColumnName = unboxOption r.[4]
                               ColumnsSpec = unboxOption r.[5]
                               Formula = unboxOption r.[6]
                               FormulaDeps = unboxOption r.[7] |})
                            "SELECT Code, DataType, Type, TableName, ColumnName, ColumnsSpec, Formula, FormulaDeps FROM VW_ElemDefinitions"
                            []

                    results
                    |> Seq.map (fun item ->
                        let elemCode = ElemCode(item.Code)
                        { Code = elemCode
                          Type =
                              match item.Type with
                              | "Formula" ->
                                  Formula
                                      { Formula = item.Formula.Value
                                        Deps = (splitDeps item.FormulaDeps) }
                              | "Db" ->
                                  DbScalar
                                      { TableName = item.TableName.Value
                                        ColumnName = item.ColumnName.Value }
                              | "DbCollection" ->
                                  DbCollection
                                      { TableName = item.TableName.Value
                                        Columns = JsonConvert.DeserializeObject<DbColumnDefinition list> item.ColumnsSpec.Value }
                              | _ -> failwith "DB configuration errror"
                          DataType = Type.GetType(item.DataType) })
                    |> ElemDefinitionStore.create

                cache <- Some store
                store


        let save (connectionString: string) (sideEffect: ElemDefinitionStoreRepo.SaveElemDefinitionStoreSideEffect) =
            let insertElemDefinition elemDefinition =
                let (ElemCode code) = elemDefinition.Code
                let dataType = elemDefinition.DataType.FullName

                let executeCommand =
                    SqlCommandHelper.executeScalar connectionString

                let id =
                    executeCommand
                        "INSERT INTO ElemDefinition(Code, DataType) OUTPUT INSERTED.ElemDefinitionId VALUES (@Code, @DataType)"
                        [ "@Code", box code
                          "@DataType", box dataType ]

                unbox id

            let insertDbElemDefinition ({ TableName = tableName; ColumnName = columnName }) elemDefinitionId =
                let executeCommand =
                    SqlCommandHelper.execute connectionString

                executeCommand
                    "INSERT INTO DbElemDefinition(TableName, ColumnName, ElemDefinitionId) VALUES (@TableName, @ColumnName, @ElemDefinitionId)"
                    [ "@TableName", box tableName
                      "@ColumnName", box columnName
                      "@ElemDefinitionId", box elemDefinitionId ]

            let insertDependency dependencyElemCode elemDefinitionId =
                let executeCommand =
                    SqlCommandHelper.execute connectionString
                
                executeCommand
                    "INSERT INTO ElemDependency(ElemDefinitionId, DependencyElemDefinitionId) VALUES (@ElemDefinitionId, (SELECT TOP 1 ElemDefinitionId FROM ElemDefinition WHERE Code = @DepCode))"
                    [ "@DepCode", box dependencyElemCode
                      "@ElemDefinitionId", box elemDefinitionId ]


            let insertFormulaElemDefinition ({ Formula = formula; Deps = deps }) elemDefinitionId =
                let executeCommand =
                    SqlCommandHelper.execute connectionString
                
                for code in deps do
                    insertDependency code elemDefinitionId

                executeCommand
                    "INSERT INTO FromulaElemDefinition(Formula, ElemDefinitionId) VALUES (@Formula, @ElemDefinitionId)"
                    [ "@Formula", box formula
                      "@ElemDefinitionId", box elemDefinitionId ]


            let processEvent event =
                match event with
                | ElemDefinitionAdded (_elemDefinitionStoreId, elemDefinition) ->
                    let elemDefinitionId = insertElemDefinition elemDefinition
                    match elemDefinition.Type with
                    | DbScalar dbElemDefinition -> insertDbElemDefinition dbElemDefinition elemDefinitionId
                    | Formula formulaElemDefinition ->
                        insertFormulaElemDefinition formulaElemDefinition elemDefinitionId
                | ElemDefinitionStoreCreated _ -> ()

            let (ElemDefinitionStoreRepo.SaveElemDefinitionStoreSideEffect (_store, events)) = sideEffect
            events |> List.map processEvent |> ignore
            cache <- None

    module DbElemValue =
        open System.Collections.Generic
        open System.Data.SqlClient
        open Core

        let loadScalar (connectionString: string)
                      ({ Definition = definition; Context = context }: HrAdmin.LoadScalarSideEffect)
                      : Result<obj, string> =
            let executeCommand =
                SqlCommandHelper.executeScalar connectionString

            let { TableName = table; ColumnName = column } = definition
            let (ContractId contractId), (YearMonth (year, month)) = context

            let result =
                executeCommand
                    (sprintf
                        "SELECT TOP 1 %s FROM %s WHERE ContractId=@ContractId AND Month=@Month AND Year=@Year"
                         column
                         table)
                    [ "@ContractId", box contractId
                      "@Month", box month
                      "@Year", box year ]

            Result.Ok result

        let loadCollection (connectionString: string)
                      ({ Definition = definition; Context = context }: HrAdmin.LoadCollectionSideEffect)
                      : Result<obj[] list, string> =
            let read =
                SqlCommandHelper.read connectionString

            let { TableName = table; Columns = columnDefs } = definition
            let (ContractId contractId), (YearMonth (year, month)) = context

            
            let columnNames = columnDefs |> List.map (fun def -> def.ColumnName) |> List.sort |> String.concat (", ")
            let columnCount = columnDefs.Length

            let result =
                read (fun r -> [| for i in [1..columnCount] do r.[i-1] |]) 
                    (sprintf
                        "SELECT %s FROM %s WHERE ContractId=@ContractId AND Month=@Month AND Year=@Year"
                         columnNames
                         table)
                    [ "@ContractId", box contractId
                      "@Month", box month
                      "@Year", box year ]

            Result.Ok result

        let getOtherEmployeeContracts (connectionString: string)
                      (HrAdmin.GetOtherEmployeeContractsSideEffect((ContractId contractId)))
                      : ContractId list =
            let read = SqlCommandHelper.read connectionString

            let results =
                read (fun r -> unbox r.[0] )
                    "SELECT DISTINCT ContractId FROM Salarii 
                        WHERE PersonId = (SELECT TOP 1 PersonId from Salarii WHERE ContractId = @ContractId)
                        AND ContractId != @ContractId"
                    ["@ContractId", box contractId]

            results
            |> List.map ContractId

    
        let getAllEmployeeContracts (connectionString: string)
                      (HrAdmin.GetAllEmployeeContractsSideEffect((ContractId contractId), (YearMonth (year, month))))
                      : ContractId list =
            let read = SqlCommandHelper.read connectionString

            let results =
                read (fun r -> unbox r.[0] )
                    "SELECT ContractId from hr.Contract 
                        WHERE OwnerDataId = (SELECT TOP 1 OwnerDataId from hr.Contract WHERE [Year] = @Year and [Month] = @Month and ContractId = @ContractId)
	                    AND [Year] = @Year and [Month] = @Month "
                    [
                        "@ContractId", box contractId
                        "@Year", box year
                        "@Month", box month
                    ]

            results
            |> List.map ContractId
