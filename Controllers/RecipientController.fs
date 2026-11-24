namespace PackstationFsharp.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open PackstationFsharp.Models
open PackstationFsharp.Repositories

type RecipientController(
    parcelRepository: IParcelRepository,
    lockerRepository: ILockerRepository,
    emailSender: IEmailSender
) =
    inherit Controller()

    member private this.CurrentUserId() =
        this.HttpContext.Session.GetString("UserId") |> Option.ofObj |> Option.defaultValue ""

    member private this.RequireLogin() =
        not (String.IsNullOrEmpty(this.CurrentUserId()))

    member this.Dashboard() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                // For demo, show all deposited/awaiting - in real app filter by email
                let! all = parcelRepository.GetAllParcelsAsync()

                let parcels =
                    ResizeArray(
                        all
                        |> Seq.filter (fun p ->
                            p.Status = ParcelStatus.Deposited || p.Status = ParcelStatus.AwaitingPickup)
                    )

                return this.View(parcels) :> IActionResult
        }

    [<HttpGet>]
    member this.Track() : IActionResult =
        this.View() :> IActionResult

    [<HttpPost>]
    member this.Track(trackingNumber: string) : Task<IActionResult> =
        task {
            if String.IsNullOrEmpty(trackingNumber) then
                this.TempData.["Error"] <- box "Bitte Trackingnummer eingeben."
                return this.View() :> IActionResult
            else
                let! parcel = parcelRepository.GetParcelByTrackingAsync(trackingNumber)

                match parcel with
                | None ->
                    this.TempData.["Error"] <- box "Paket nicht gefunden."
                    return this.View() :> IActionResult
                | Some p ->
                    this.ViewData.["Parcel"] <- box p

                    if not (String.IsNullOrEmpty(p.LockerId)) then
                        let! locker = lockerRepository.GetLockerByIdAsync(p.LockerId)

                        match locker with
                        | Some l -> this.ViewData.["Locker"] <- box l
                        | None -> ()

                    if not (String.IsNullOrEmpty(p.CompartmentId)) then
                        let! comps = lockerRepository.GetCompartmentsByLockerAsync(p.LockerId)

                        let comp =
                            comps |> Seq.tryFind (fun c -> c.CompartmentId = p.CompartmentId)

                        match comp with
                        | Some c -> this.ViewData.["Compartment"] <- box c
                        | None -> ()

                    let! pickupCode = parcelRepository.GetLatestCodeForParcelAsync(p.ParcelId)

                    match pickupCode with
                    | Some code -> this.ViewData.["PickupCode"] <- box code
                    | None -> ()

                    return this.View() :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.ConfirmPickup(parcelId: string) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcel = parcelRepository.GetParcelByIdAsync(parcelId)

                match parcel with
                | None -> return this.NotFound() :> IActionResult
                | Some p ->
                    p.Status <- ParcelStatus.AwaitingPickup
                    p.AwaitingPickupAt <- Nullable(DateTime.Now)
                    do! parcelRepository.UpdateParcelAsync(p)

                    // Generate pickup code
                    let code = PickupCode()
                    code.CodeId <- Guid.NewGuid().ToString()
                    code.ParcelId <- p.ParcelId
                    code.CompartmentId <- (if isNull p.CompartmentId then "" else p.CompartmentId)
                    code.Code <- Random.Shared.Next(100000, 999999).ToString()
                    code.CreatedAt <- DateTime.Now
                    code.ExpiresAt <- DateTime.Now.AddMinutes(60.0)
                    code.IsUsed <- false
                    do! parcelRepository.AddPickupCodeAsync(code)

                    let! locker = lockerRepository.GetLockerByIdAsync(if isNull p.LockerId then "" else p.LockerId)

                    let! comps =
                        lockerRepository.GetCompartmentsByLockerAsync(if isNull p.LockerId then "" else p.LockerId)

                    let comp =
                        comps |> Seq.tryFind (fun c -> c.CompartmentId = p.CompartmentId)

                    if not (String.IsNullOrEmpty(p.RecipientEmail)) then
                        let lockerName =
                            match locker with
                            | Some l -> l.LockerName
                            | None -> null

                        let lockerAddress =
                            match locker with
                            | Some l -> l.Address
                            | None -> null

                        let compartmentNumber =
                            match comp with
                            | Some c -> c.CompartmentNumber.ToString()
                            | None -> ""

                        let body =
                            $"""<h2>Ihr Abholcode</h2>
                            <p>Trackingnummer: {p.TrackingNumber}</p>
                            <p>Packstation: {lockerName}</p>
                            <p>Adresse: {lockerAddress}</p>
                            <p>Fachnummer: {compartmentNumber}</p>
                            <p><strong>Abholcode: {code.Code}</strong></p>
                            <p>Gültig bis: {code.ExpiresAt.ToString("dd.MM.yyyy HH:mm")}</p>"""

                        do! emailSender.SendEmailAsync(p.RecipientEmail, "Packstation - Ihr Abholcode", body)

                    this.TempData.["Message"] <-
                        box $"Abholcode {code.Code} wurde generiert und per E-Mail gesendet."

                    let routeValues = RouteValueDictionary()
                    routeValues.["trackingNumber"] <- p.TrackingNumber
                    return this.RedirectToAction("Track", routeValues) :> IActionResult
        }
