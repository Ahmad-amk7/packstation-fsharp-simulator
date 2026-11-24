namespace PackstationFsharp.Models

open System

type UserAccount() =
    member val UserId : string = Guid.NewGuid().ToString() with get, set
    member val UserName : string = null with get, set
    member val UserEmail : string = null with get, set
    member val UserPassword : string = null with get, set
    member val Certificate : string = String.Empty with get, set
    member val IsActivated : bool = false with get, set
    member val IsRestricted : bool = false with get, set
    member val Role : UserRoleType = UserRoleType.User with get, set
    member val PhoneNumber : string = null with get, set
    member val ProfileImage : string = null with get, set
    member val AddressStreet : string = null with get, set
    member val AddressHouseNo : string = null with get, set
    member val AddressCity : string = null with get, set
    member val AddressPostalCode : string = null with get, set
    member val AddressCountry : string = null with get, set
    member val CreatedAt : DateTime = DateTime.Now with get, set
