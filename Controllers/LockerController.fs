namespace PackstationFsharp.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open PackstationFsharp.Models
open PackstationFsharp.Repositories
open Renci.SshNet

type LockerController(
    parcelRepository: IParcelRepository,
    lockerRepository: ILockerRepository,
    configuration: IConfiguration,
    logger: ILogger<LockerController>
) =
    inherit Controller()

    member private this.CurrentUserId() =
        this.HttpContext.Session.GetString("UserId") |> Option.ofObj |> Option.defaultValue ""

    member private this.TempString(key: string) =
        match this.TempData.[key] with
        | null -> ""
        | value -> Convert.ToString(value)

    member private this.RemoteIpAddress() =
        match this.HttpContext.Connection.RemoteIpAddress with
        | null -> null
        | ip -> ip.ToString()

    member private _.SetServo(compartmentId: string, action: string) : Task<bool> =
        task {
            let mutable succeeded = false

            try
                let! compartment = lockerRepository.GetCompartmentByIdAsync(compartmentId)

                match compartment with
                | None -> invalidOp "The configured compartment was not found."
                | Some item ->
                    let channel = item.CompartmentNumber - 1

                    if channel < 0 || channel > 15 then
                        invalidOp "The compartment number does not map to a servo channel."

                    let host = configuration.["RaspberryPi:Host"]
                    let username = configuration.["RaspberryPi:User"]
                    let port = configuration.GetValue<int>("RaspberryPi:Port")
                    let keyPath = configuration.["RaspberryPi:PrivateKeyPath"]
                    let fingerprint = configuration.["RaspberryPi:HostKeyFingerprint"]
                    let pythonPath = configuration.["RaspberryPi:PythonPath"]
                    let scriptPath = configuration.["RaspberryPi:ScriptPath"]

                    if
                        [ host; username; keyPath; fingerprint; pythonPath; scriptPath ]
                        |> List.exists String.IsNullOrWhiteSpace
                        || port < 1
                        || port > 65535
                    then
                        invalidOp "Raspberry Pi SSH settings are not configured."

                    use keyFile = new PrivateKeyFile(keyPath)
                    let authentication =
                        PrivateKeyAuthenticationMethod(username, [| keyFile :> IPrivateKeySource |])
                        :> AuthenticationMethod
                    let connectionInfo = new ConnectionInfo(host, port, username, [| authentication |])
                    use client = new SshClient(connectionInfo)

                    client.HostKeyReceived.Add(fun args ->
                        let actualFingerprint =
                            args.FingerPrint
                            |> Array.map (fun value -> value.ToString("x2"))
                            |> String.concat ":"

                        args.CanTrust <-
                            String.Equals(actualFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))

                    client.Connect()

                    let quote (value: string) =
                        "'" + value.Replace("'", "'\\''") + "'"

                    let command =
                        sprintf "%s %s %d %s" (quote pythonPath) (quote scriptPath) channel action

                    let result = client.RunCommand(command)
                    client.Disconnect()
                    succeeded <- result.ExitStatus = Nullable<int>(0)
            with
            | :? Renci.SshNet.Common.SshException
            | :? IOException
            | :? UnauthorizedAccessException
            | :? ArgumentException
            | :? InvalidOperationException
            | :? FormatException
            | :? System.Net.Sockets.SocketException
            | :? TimeoutException
            | :? System.Security.Cryptography.CryptographicException ->
                logger.LogWarning("The Raspberry Pi servo command failed; verify server-side SSH settings and connectivity.")

            return succeeded
        }

    member private this.HardwareError() =
        this.TempData.["Error"] <- box "Das Fach konnte nicht gesteuert werden. Bitte prüfen Sie die SSH-Konfiguration."

    [<HttpGet>]
    member this.Open() : IActionResult =
        this.View() :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Open(code: string) : Task<IActionResult> =
        task {
            if String.IsNullOrEmpty(code) || code.Length <> 6 then
                this.TempData.["Error"] <- box "Bitte geben Sie einen gültigen 6-stelligen Code ein."
                return this.View() :> IActionResult
            else
                let! pickupCode = parcelRepository.GetValidCodeAsync(code)

                match pickupCode with
                | None ->
                    this.TempData.["Error"] <- box "Ungültiger oder abgelaufener Code."
                    return this.View() :> IActionResult
                | Some pc ->
                    let! parcel = parcelRepository.GetParcelByIdAsync(pc.ParcelId)

                    match parcel with
                    | None ->
                        this.TempData.["Error"] <- box "Paket nicht gefunden."
                        return this.View() :> IActionResult
                    | Some p ->
                        this.ViewData.["Parcel"] <- box p
                        this.ViewData.["PickupCode"] <- box pc
                        this.ViewData.["CompartmentId"] <- box pc.CompartmentId
                        this.TempData.["OpenCode"] <- box code
                        this.TempData.["CompartmentId"] <- box pc.CompartmentId
                        return this.View("OpenConfirm") :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Unlock1() : Task<IActionResult> =
        task {
            let code = this.TempString("OpenCode")
            let compId = this.TempString("CompartmentId")
            let! pickupCode = parcelRepository.GetValidCodeAsync(code)

            match pickupCode with
            | None ->
                this.TempData.["Error"] <- box "Ungültiger Code."
                return this.RedirectToAction("Open") :> IActionResult
            | Some pc ->
                let! parcel = parcelRepository.GetParcelByIdAsync(pc.ParcelId)

                match parcel with
                | None -> return this.RedirectToAction("Open") :> IActionResult
                | Some p ->
                    let! opened = this.SetServo(compId, "open")

                    if not opened then
                        this.HardwareError()
                        return this.RedirectToAction("Open") :> IActionResult
                    else
                        let log = PickupLog()
                        log.LogId <- Guid.NewGuid().ToString()
                        log.ParcelId <- p.ParcelId
                        log.CompartmentId <- compId
                        log.UserId <- this.CurrentUserId()
                        log.Action <- "Open"
                        log.ActionAt <- DateTime.Now
                        log.IpAddress <- this.RemoteIpAddress()
                        log.Details <- "Fach geöffnet"
                        do! parcelRepository.AddPickupLogAsync(log)

                        this.ViewData.["Parcel"] <- box p
                        this.ViewData.["CompartmentId"] <- box compId
                        this.TempData.["ParcelIdForPickup"] <- box p.ParcelId
                        this.TempData.["CompIdForPickup"] <- box compId
                        this.TempData.["CodeIdForPickup"] <- box pc.CodeId
                        return this.View("Unlock") :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.ConfirmReceived() : Task<IActionResult> =
        task {
            let parcelId = this.TempString("ParcelIdForPickup")
            let compId = this.TempString("CompIdForPickup")
            let codeId = this.TempString("CodeIdForPickup")
            let! parcel = parcelRepository.GetParcelByIdAsync(parcelId)

            let mutable hardwareFailed = false

            match parcel with
            | Some p ->
                let! closed = this.SetServo(compId, "close")

                if not closed then
                    this.HardwareError()
                    hardwareFailed <- true
                else
                    p.Status <- ParcelStatus.PickedUp
                    p.PickedUpAt <- Nullable(DateTime.Now)
                    do! parcelRepository.UpdateParcelAsync(p)
                    do! lockerRepository.UpdateCompartmentStatusAsync(compId, CompartmentStatus.Free, null)
                    do! parcelRepository.MarkCodeUsedAsync(codeId)

                    let log = PickupLog()
                    log.LogId <- Guid.NewGuid().ToString()
                    log.ParcelId <- p.ParcelId
                    log.CompartmentId <- compId
                    log.UserId <- this.CurrentUserId()
                    log.Action <- "PickedUp"
                    log.ActionAt <- DateTime.Now
                    log.IpAddress <- this.RemoteIpAddress()
                    log.Details <- "Paket entnommen - Fach freigegeben"
                    do! parcelRepository.AddPickupLogAsync(log)
            | None -> ()

            if hardwareFailed then
                return this.RedirectToAction("Open") :> IActionResult
            else
                this.TempData.["Message"] <-
                    box "Vielen Dank! Das Paket wurde als abgeholt markiert. Das Fach wird geschlossen."

                return this.RedirectToAction("Open") :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.AutoClose() : Task<IActionResult> =
        task {
            let parcelId = this.TempString("ParcelIdForPickup")
            let compId = this.TempString("CompIdForPickup")
            let! parcel = parcelRepository.GetParcelByIdAsync(parcelId)

            let mutable hardwareFailed = false

            match parcel with
            | Some p ->
                let! closed = this.SetServo(compId, "close")

                if not closed then
                    this.HardwareError()
                    hardwareFailed <- true
                else
                    let log = PickupLog()
                    log.LogId <- Guid.NewGuid().ToString()
                    log.ParcelId <- p.ParcelId
                    log.CompartmentId <- compId
                    log.UserId <- this.CurrentUserId()
                    log.Action <- "AutoClose"
                    log.ActionAt <- DateTime.Now
                    log.IpAddress <- this.RemoteIpAddress()
                    log.Details <- "Automatisches Schließen (Timeout)"
                    do! parcelRepository.AddPickupLogAsync(log)
            | None -> ()

            if hardwareFailed then
                return this.RedirectToAction("Open") :> IActionResult
            else
                this.TempData.["Message"] <- box "Das Fach wurde automatisch geschlossen."
                return this.RedirectToAction("Open") :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Unlock() : Task<IActionResult> =
        task {
            let code = this.TempString("OpenCode")
            let compId = this.TempString("CompartmentId")
            this.TempData.["CompartmentId"] <- box compId
            this.TempData.["OpenCode"] <- box code
            let! pickupCode = parcelRepository.GetValidCodeAsync(code)

            match pickupCode with
            | None ->
                this.TempData.["Error"] <- box "Ungültiger Code."
                return this.RedirectToAction("Open") :> IActionResult
            | Some pc ->
                let! parcel = parcelRepository.GetParcelByIdAsync(pc.ParcelId)

                match parcel with
                | None -> return this.RedirectToAction("Open") :> IActionResult
                | Some p ->
                    let! opened = this.SetServo(compId, "open")

                    if not opened then
                        this.HardwareError()
                        return this.RedirectToAction("Open") :> IActionResult
                    else
                        let log = PickupLog()
                        log.LogId <- Guid.NewGuid().ToString()
                        log.ParcelId <- p.ParcelId
                        log.CompartmentId <- compId
                        log.UserId <- this.CurrentUserId()
                        log.Action <- "Open"
                        log.ActionAt <- DateTime.Now
                        log.IpAddress <- this.RemoteIpAddress()
                        log.Details <- "Fach geöffnet"
                        do! parcelRepository.AddPickupLogAsync(log)

                        this.ViewData.["Parcel"] <- box p
                        this.ViewData.["CompartmentId"] <- box compId
                        this.TempData.["ParcelIdForPickup"] <- box p.ParcelId
                        this.TempData.["CompIdForPickup"] <- box compId
                        this.TempData.["CodeIdForPickup"] <- box pc.CodeId
                        return this.View("Unlock") :> IActionResult
        }
