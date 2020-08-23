namespace LambdaPayroll.PublishedLanguage

open NBB.Application.DataContracts

type AddDbElemDefinition(elemCode, table, column, dataType) =
    inherit Command ()     
    member _.ElemCode with get() : string = elemCode
    member _.Table with get() : string = table
    member _.Column with get() : string = column
    member _.DataType with get() : string = dataType

type AddFormulaElemDefinition(elemCode, formula, dataType) =
    inherit Command ()     
    member _.ElemCode with get() : string = elemCode
    member _.Formula with get() : string = formula    
    member _.DataType with get() : string = dataType