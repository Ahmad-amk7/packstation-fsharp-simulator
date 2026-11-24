namespace PackstationFsharp.Models

open System

type SubscriptionPlan() =
    member val PlanId : string = Guid.NewGuid().ToString() with get, set
    member val PlanName : string = null with get, set
    member val Price : decimal = 0M with get, set
    member val ParcelLimit : int = 0 with get, set
    member val DurationDays : int = 0 with get, set
    member val Description : string = null with get, set
    member val IsActive : bool = true with get, set

type UserSubscription() =
    member val SubscriptionId : string = Guid.NewGuid().ToString() with get, set
    member val UserId : string = String.Empty with get, set
    member val PlanId : string = String.Empty with get, set
    member val StartDate : DateTime = DateTime.Now with get, set
    member val EndDate : DateTime = DateTime.Now with get, set
    member val Status : SubscriptionStatus = SubscriptionStatus.Active with get, set
    member val ParcelsUsed : int = 0 with get, set
