namespace PackstationFsharp.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open PackstationFsharp.Models
open PackstationFsharp.Repositories

type EmployeeController(
    parcelRepository: IParcelRepository,
    lockerRepository: ILockerRepository,
    userRepository: IUserRepository,
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
                let! allParcels = parcelRepository.GetAllParcelsAsync()
                let count status = allParcels |> Seq.filter (fun p -> p.Status = status) |> Seq.length

                this.ViewData.["Created"] <- box (count ParcelStatus.Created)
                this.ViewData.["InTransit"] <- box (count ParcelStatus.InTransit)
                this.ViewData.["Deposited"] <- box (count ParcelStatus.Deposited)
                this.ViewData.["AwaitingPickup"] <- box (count ParcelStatus.AwaitingPickup)
                this.ViewData.["PickedUp"] <- box (count ParcelStatus.PickedUp)

                let latest =
                    ResizeArray(allParcels |> Seq.sortByDescending (fun p -> p.CreatedAt) |> Seq.truncate 50)

                return this.View(latest) :> IActionResult
        }

    [<HttpGet>]
    member this.Parcels() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcels = parcelRepository.GetAllParcelsAsync()

                let sorted =
                    ResizeArray(parcels |> Seq.sortByDescending (fun p -> p.CreatedAt))

                return this.View(sorted) :> IActionResult
        }

    [<HttpGet>]
    member this.Deposit(trackingNumber: string) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! lockersList = lockerRepository.GetAllLockersAsync()
                let mutable lockers = lockersList

                // Ensure at least one locker exists
                if lockers.Count = 0 then
                    let locker = Locker()
                    locker.LockerId <- Guid.NewGuid().ToString()
                    locker.LockerName <- "Packstation 1"
                    locker.Address <- "Hauptstraße 1"
                    locker.IpAddress <- "127.0.0.1"
                    locker.Port <- 5001
                    locker.ApiKey <- Guid.NewGuid().ToString()
                    locker.Status <- LockerStatus.Active
                    locker.UseSimulation <- true
                    locker.CreatedAt <- DateTime.Now
                    do! lockerRepository.AddLockerAsync(locker)

                    for i in 1..10 do
                        let compartment = Compartment()
                        compartment.CompartmentId <- Guid.NewGuid().ToString()
                        compartment.LockerId <- locker.LockerId
                        compartment.CompartmentNumber <- i
                        compartment.Status <- CompartmentStatus.Free
                        do! lockerRepository.AddCompartmentAsync(compartment)

                    let! refreshed = lockerRepository.GetAllLockersAsync()
                    lockers <- refreshed

                if String.IsNullOrEmpty(trackingNumber) then
                    this.ViewData.["Lockers"] <- box lockers
                    return this.View() :> IActionResult
                else
                    let! parcel = parcelRepository.GetParcelByTrackingAsync(trackingNumber)

                    match parcel with
                    | None ->
                        this.TempData.["Error"] <- box "Paket nicht gefunden."
                        this.ViewData.["Lockers"] <- box lockers
                        return this.View() :> IActionResult
                    | Some p ->
                        this.ViewData.["Parcel"] <- box p
                        this.ViewData.["Lockers"] <- box lockers
                        return this.View() :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Deposit(trackingNumber: string, lockerId: string) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcel = parcelRepository.GetParcelByTrackingAsync(trackingNumber)

                match parcel with
                | None ->
                    this.TempData.["Error"] <- box "Paket nicht gefunden."

                    let routeValues = RouteValueDictionary()
                    routeValues.["trackingNumber"] <- trackingNumber
                    return this.RedirectToAction("Deposit", routeValues) :> IActionResult
                | Some p ->
                    let! freeCompOpt = lockerRepository.GetFreeCompartmentAsync(lockerId)
                    let mutable freeComp = freeCompOpt

                    if freeComp.IsNone then
                        // Try to ensure compartments exist
                        let! compsExist = lockerRepository.GetCompartmentsByLockerAsync(lockerId)

                        if compsExist.Count = 0 then
                            for i in 1..10 do
                                let compartment = Compartment()
                                compartment.CompartmentId <- Guid.NewGuid().ToString()
                                compartment.LockerId <- lockerId
                                compartment.CompartmentNumber <- i
                                compartment.Status <- CompartmentStatus.Free
                                do! lockerRepository.AddCompartmentAsync(compartment)

                            let! retry = lockerRepository.GetFreeCompartmentAsync(lockerId)
                            freeComp <- retry

                    match freeComp with
                    | None ->
                        this.TempData.["Error"] <- box "Keine freien Fächer in dieser Packstation."

                        let routeValues = RouteValueDictionary()
                        routeValues.["trackingNumber"] <- trackingNumber
                        return this.RedirectToAction("Deposit", routeValues) :> IActionResult
                    | Some free ->
                        let! locker = lockerRepository.GetLockerByIdAsync(lockerId)

                        p.Status <- ParcelStatus.Deposited
                        p.LockerId <- lockerId
                        p.CompartmentId <- free.CompartmentId
                        p.DepositedByEmployeeId <- this.CurrentUserId()
                        p.DepositedAt <- Nullable(DateTime.Now)
                        do! parcelRepository.UpdateParcelAsync(p)
                        do! lockerRepository.UpdateCompartmentStatusAsync(free.CompartmentId, CompartmentStatus.Occupied, p.ParcelId)

                        // Send email to recipient
                        if not (String.IsNullOrEmpty(p.RecipientEmail)) then
                            let lockerName =
                                match locker with
                                | Some l -> l.LockerName
                                | None -> null

                            let lockerAddress =
                                match locker with
                                | Some l -> l.Address
                                | None -> null

                            let body =
                                $"""<h2>Ihr Paket ist in der Packstation angekommen</h2>
                                <p>Trackingnummer: {p.TrackingNumber}</p>
                                <p>Packstation: {lockerName}</p>
                                <p>Adresse: {lockerAddress}</p>
                                <p>Fachnummer: {free.CompartmentNumber}</p>
                                <p>Bitte bestätigen Sie den Erhalt in Ihrem Konto, um den Abholcode zu erhalten.</p>"""

                            do! emailSender.SendEmailAsync(p.RecipientEmail, "Packstation - Paket bereit zur Abholung", body)

                        this.TempData.["Message"] <-
                            box $"Paket erfolgreich in Fach {free.CompartmentNumber} eingelegt."

                        return this.RedirectToAction("Parcels") :> IActionResult
        }

    member this.InTransit() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcels = parcelRepository.GetParcelsByStatusAsync(ParcelStatus.InTransit)
                return this.View(parcels) :> IActionResult
        }

    member this.Deposited() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcels = parcelRepository.GetParcelsByStatusAsync(ParcelStatus.Deposited)
                return this.View(parcels) :> IActionResult
        }

    member this.AwaitingPickup() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcels = parcelRepository.GetParcelsByStatusAsync(ParcelStatus.AwaitingPickup)
                return this.View(parcels) :> IActionResult
        }
