namespace PackstationFsharp.Repositories

open System.Threading.Tasks
open PackstationFsharp.Models

type IParcelRepository =
    abstract AddParcelAsync : Parcel -> Task<unit>
    abstract GetParcelByTrackingAsync : string -> Task<Parcel option>
    abstract GetParcelByIdAsync : string -> Task<Parcel option>
    abstract GetParcelsBySenderAsync : string -> Task<ResizeArray<Parcel>>
    abstract GetAllParcelsAsync : unit -> Task<ResizeArray<Parcel>>
    abstract GetParcelsByStatusAsync : ParcelStatus -> Task<ResizeArray<Parcel>>
    abstract UpdateParcelAsync : Parcel -> Task<unit>
    abstract GetValidCodeAsync : string -> Task<PickupCode option>
    abstract GetLatestCodeForParcelAsync : string -> Task<PickupCode option>
    abstract AddPickupCodeAsync : PickupCode -> Task<unit>
    abstract MarkCodeUsedAsync : string -> Task<unit>
    abstract AddPickupLogAsync : PickupLog -> Task<unit>
    abstract GetLogsByParcelAsync : string -> Task<ResizeArray<PickupLog>>
