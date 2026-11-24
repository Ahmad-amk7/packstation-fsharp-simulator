namespace PackstationFsharp.Controllers

open System.Diagnostics
open Microsoft.AspNetCore.Mvc
open PackstationFsharp.Models

type HomeController() =
    inherit Controller()

    member this.Index() : IActionResult =
        this.View() :> IActionResult

    member this.Privacy() : IActionResult =
        this.View() :> IActionResult

    [<ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)>]
    member this.Error() : IActionResult =
        let requestId =
            match Activity.Current with
            | null -> this.HttpContext.TraceIdentifier
            | activity -> activity.Id

        this.View(ErrorViewModel(RequestId = requestId)) :> IActionResult
