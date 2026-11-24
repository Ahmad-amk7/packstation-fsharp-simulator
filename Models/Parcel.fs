namespace PackstationFsharp.Models

open System

module ParcelHelpers =
    let generateTracking () =
        "PS" + DateTime.Now.ToString("yyMMddHHmmss") + Random.Shared.Next(1000, 9999).ToString()

    let generateCode () =
        Random.Shared.Next(100000, 999999).ToString()

type Parcel() =
    member val ParcelId : string = Guid.NewGuid().ToString() with get, set
    member val TrackingNumber : string = ParcelHelpers.generateTracking () with get, set
    member val SenderUserId : string = String.Empty with get, set

    // Recipient details
    member val RecipientFullName : string = null with get, set
    member val RecipientEmail : string = null with get, set
    member val RecipientPhone : string = null with get, set
    member val RecipientStreet : string = null with get, set
    member val RecipientHouseNo : string = null with get, set
    member val RecipientCity : string = null with get, set
    member val RecipientPostalCode : string = null with get, set
    member val RecipientCountry : string = null with get, set

    member val ParcelDescription : string = null with get, set
    member val ParcelImage : string = null with get, set
    member val Status : ParcelStatus = ParcelStatus.Created with get, set

    member val LockerId : string = null with get, set
    member val CompartmentId : string = null with get, set
    member val DepositedByEmployeeId : string = null with get, set
    member val CreatedAt : DateTime = DateTime.Now with get, set
    member val DepositedAt : Nullable<DateTime> = Nullable() with get, set
    member val AwaitingPickupAt : Nullable<DateTime> = Nullable() with get, set
    member val PickedUpAt : Nullable<DateTime> = Nullable() with get, set
    member val AutoClosedAt : Nullable<DateTime> = Nullable() with get, set
    member val Notes : string = null with get, set

type PickupCode() =
    member val CodeId : string = Guid.NewGuid().ToString() with get, set
    member val ParcelId : string = String.Empty with get, set
    member val CompartmentId : string = String.Empty with get, set
    member val Code : string = ParcelHelpers.generateCode () with get, set
    member val CreatedAt : DateTime = DateTime.Now with get, set
    member val ExpiresAt : DateTime = DateTime.Now.AddMinutes(60.0) with get, set
    member val IsUsed : bool = false with get, set
    member val UsedAt : Nullable<DateTime> = Nullable() with get, set

type PickupLog() =
    member val LogId : string = Guid.NewGuid().ToString() with get, set
    member val ParcelId : string = String.Empty with get, set
    member val CompartmentId : string = String.Empty with get, set
    member val UserId : string = null with get, set
    // Open, Close, PickedUp, AutoClose
    member val Action : string = String.Empty with get, set
    member val ActionAt : DateTime = DateTime.Now with get, set
    member val IpAddress : string = null with get, set
    member val Details : string = null with get, set
