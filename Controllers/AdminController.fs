namespace PackstationFsharp.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open PackstationFsharp.Models
open PackstationFsharp.Repositories

type AdminController(
    userRepository: IUserRepository,
    lockerRepository: ILockerRepository,
    parcelRepository: IParcelRepository
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
                let! users = userRepository.GetAllUsersAsync()
                let! parcels = parcelRepository.GetAllParcelsAsync()
                let! lockers = lockerRepository.GetAllLockersAsync()
                this.ViewData.["UserCount"] <- box users.Count
                this.ViewData.["ParcelCount"] <- box parcels.Count
                this.ViewData.["LockerCount"] <- box lockers.Count
                return this.View(ResizeArray(parcels |> Seq.truncate 20)) :> IActionResult
        }

    member this.Users() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! users = userRepository.GetAllUsersAsync()
                return this.View(users) :> IActionResult
        }

    member this.Lockers() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! lockers = lockerRepository.GetAllLockersAsync()
                return this.View(lockers) :> IActionResult
        }

    [<HttpGet>]
    member this.CreateLocker() : IActionResult =
        if not (this.RequireLogin()) then
            this.RedirectToAction("Login", "User") :> IActionResult
        else
            this.View(Locker()) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.CreateLocker(locker: Locker) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                locker.LockerId <- Guid.NewGuid().ToString()
                locker.ApiKey <- Guid.NewGuid().ToString()
                locker.Status <- LockerStatus.Active
                locker.UseSimulation <- true
                locker.CreatedAt <- DateTime.Now
                do! lockerRepository.AddLockerAsync(locker)
                this.TempData.["Message"] <- box "Packstation erfolgreich erstellt."
                return this.RedirectToAction("Lockers") :> IActionResult
        }

    [<HttpGet>]
    member this.LockerDetails(id: string) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! locker = lockerRepository.GetLockerByIdAsync(id)

                match locker with
                | None -> return this.NotFound() :> IActionResult
                | Some l ->
                    let! compartments = lockerRepository.GetCompartmentsByLockerAsync(id)
                    this.ViewData.["Compartments"] <- box compartments
                    return this.View(l) :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.AddCompartments(lockerId: string, count: int) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                for i in 1..count do
                    let compartment = Compartment()
                    compartment.CompartmentId <- Guid.NewGuid().ToString()
                    compartment.LockerId <- lockerId
                    compartment.CompartmentNumber <- i
                    compartment.Status <- CompartmentStatus.Free
                    do! lockerRepository.AddCompartmentAsync(compartment)

                this.TempData.["Message"] <- box $"{count} Fächer hinzugefügt."

                let routeValues = RouteValueDictionary()
                routeValues.["id"] <- lockerId
                return this.RedirectToAction("LockerDetails", routeValues) :> IActionResult
        }
