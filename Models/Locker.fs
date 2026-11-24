namespace PackstationFsharp.Models

open System

type Locker() =
    member val LockerId : string = Guid.NewGuid().ToString() with get, set
    member val LockerName : string = null with get, set
    member val Address : string = null with get, set
    member val IpAddress : string = null with get, set
    member val Port : int = 5001 with get, set
    member val ApiKey : string = Guid.NewGuid().ToString() with get, set
    member val Status : LockerStatus = LockerStatus.Active with get, set
    member val UseSimulation : bool = true with get, set
    member val CreatedAt : DateTime = DateTime.Now with get, set

type Compartment() =
    member val CompartmentId : string = Guid.NewGuid().ToString() with get, set
    member val LockerId : string = String.Empty with get, set
    member val CompartmentNumber : int = 0 with get, set
    member val Status : CompartmentStatus = CompartmentStatus.Free with get, set
    member val CurrentParcelId : string = null with get, set
    member val OccupiedSince : Nullable<DateTime> = Nullable() with get, set
