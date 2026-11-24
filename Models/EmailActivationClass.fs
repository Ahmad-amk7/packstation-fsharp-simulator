namespace PackstationFsharp.Models

open System

type EmailActivation() =
    member val UserId : string = String.Empty with get, set
    member val UserEmail : string = String.Empty with get, set
    member val RequestDateTime : DateTime = DateTime.Now with get, set
    member val ActivationCode : string = String.Empty with get, set
    member val Certificate : string = String.Empty with get, set
