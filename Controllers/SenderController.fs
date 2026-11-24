namespace PackstationFsharp.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open PackstationFsharp.Models
open PackstationFsharp.Repositories

type SenderController(
    parcelRepository: IParcelRepository,
    userRepository: IUserRepository,
    env: IWebHostEnvironment
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
                let! parcels = parcelRepository.GetParcelsBySenderAsync(this.CurrentUserId())
                this.ViewData.["UserId"] <- box (this.CurrentUserId())
                return this.View(parcels) :> IActionResult
        }

    [<HttpGet>]
    member this.CreateParcel() : IActionResult =
        if not (this.RequireLogin()) then
            this.RedirectToAction("Login", "User") :> IActionResult
        else
            this.View(Parcel()) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.CreateParcel(parcel: Parcel, parcelImage: IFormFile) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                parcel.ParcelId <- Guid.NewGuid().ToString()
                parcel.SenderUserId <- this.CurrentUserId()
                parcel.Status <- ParcelStatus.Created
                parcel.CreatedAt <- DateTime.Now

                parcel.TrackingNumber <-
                    "PS" + DateTime.Now.ToString("yyMMddHHmmss") + Random.Shared.Next(1000, 9999).ToString()

                if not (isNull parcelImage) && parcelImage.Length > 0L then
                    let uploadsFolder = Path.Combine(env.WebRootPath, "uploads")
                    Directory.CreateDirectory(uploadsFolder) |> ignore

                    let uniqueFileName =
                        Guid.NewGuid().ToString() + "_" + Path.GetFileName(parcelImage.FileName)

                    let filePath = Path.Combine(uploadsFolder, uniqueFileName)

                    use fileStream = new FileStream(filePath, FileMode.Create)
                    do! parcelImage.CopyToAsync(fileStream)
                    parcel.ParcelImage <- "/uploads/" + uniqueFileName

                do! parcelRepository.AddParcelAsync(parcel)

                this.TempData.["Message"] <-
                    box $"Paket erfolgreich erstellt. Trackingnummer: {parcel.TrackingNumber}"

                return this.RedirectToAction("Dashboard") :> IActionResult
        }

    member this.MyParcels() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login", "User") :> IActionResult
            else
                let! parcels = parcelRepository.GetParcelsBySenderAsync(this.CurrentUserId())
                return this.View(parcels) :> IActionResult
        }
