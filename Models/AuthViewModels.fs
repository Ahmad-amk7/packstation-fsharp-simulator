namespace PackstationFsharp.Models

open System.ComponentModel.DataAnnotations

type RegisterViewModel() =
    [<Required(ErrorMessage = "Benutzername ist erforderlich")>]
    member val UserName : string = null with get, set

    [<Required(ErrorMessage = "E-Mail ist erforderlich")>]
    [<EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")>]
    member val UserEmail : string = null with get, set

    [<Required(ErrorMessage = "Passwort ist erforderlich")>]
    [<DataType(DataType.Password)>]
    member val UserPassword : string = null with get, set

    [<Required(ErrorMessage = "Passwortbestätigung ist erforderlich")>]
    [<DataType(DataType.Password)>]
    [<Compare("UserPassword", ErrorMessage = "Die Passwörter stimmen nicht überein.")>]
    member val ConfirmPassword : string = null with get, set

type LoginViewModel() =
    [<Required(ErrorMessage = "E-Mail ist erforderlich")>]
    [<EmailAddress>]
    member val Email : string = null with get, set

    [<Required(ErrorMessage = "Passwort ist erforderlich")>]
    [<DataType(DataType.Password)>]
    member val Password : string = null with get, set

type ActivateViewModel() =
    [<Required>]
    member val Email : string = null with get, set

    [<Required>]
    member val Code : string = null with get, set

type ProfileViewModel() =
    member val UserId : string = null with get, set
    member val UserName : string = null with get, set
    member val UserEmail : string = null with get, set
    member val PhoneNumber : string = null with get, set
    member val ProfileImage : string = null with get, set
    member val AddressStreet : string = null with get, set
    member val AddressHouseNo : string = null with get, set
    member val AddressCity : string = null with get, set
    member val AddressPostalCode : string = null with get, set
    member val AddressCountry : string = null with get, set
    member val Role : UserRoleType = UserRoleType.User with get, set
    member val IsActivated : bool = false with get, set
