namespace PackstationFsharp.Models

type UserRoleType =
    | User = 0
    | Sender = 1
    | Recipient = 2
    | Employee = 3
    | Admin = 4

type SubscriptionStatus =
    | Inactive = 0
    | Active = 1
    | Expired = 2

type ParcelStatus =
    | Created = 0
    | InTransit = 1
    | Deposited = 2
    | AwaitingPickup = 3
    | PickedUp = 4
    | Cancelled = 5
    | Expired = 6

type CompartmentStatus =
    | Free = 0
    | Occupied = 1
    | Reserved = 2
    | OutOfService = 3

type LockerStatus =
    | Active = 0
    | Inactive = 1
