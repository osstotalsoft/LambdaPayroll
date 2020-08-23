namespace LambdaPayroll.PublishedLanguage

open NBB.Core.Abstractions
open MediatR
open NBB.Application.DataContracts

type ElemDefinitionAdded =
    { ElemCode: string; Metadata: EventMetadata }
        
    interface INotification
    interface IMetadataProvider<EventMetadata> with
        member this.Metadata with get() = this.Metadata
    interface IEvent with
        member this.EventId with get() = this.Metadata.EventId
      

