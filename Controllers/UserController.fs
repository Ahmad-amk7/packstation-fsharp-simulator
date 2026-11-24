namespace PackstationFsharp.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Hosting
open PackstationFsharp.Models
open PackstationFsharp.Repositories

type UserController(
    userRepository: IUserRepository,
    emailActivationRepository: IEmailActivationRepository,
    env: IWebHostEnvironment
) =
    inherit Controller()

    member private this.CurrentUserId() =
        this.HttpContext.Session.GetString("UserId") |> Option.ofObj |> Option.defaultValue ""

    member private this.RequireLogin() =
        not (String.IsNullOrEmpty(this.CurrentUserId()))

    member private this.SignInUser(account: UserAccount) =
        this.HttpContext.Session.SetString("UserId", if isNull account.UserId then "" else account.UserId)
        this.HttpContext.Session.SetString("UserName", if isNull account.UserName then "" else account.UserName)
        this.HttpContext.Session.SetString("UserEmail", if isNull account.UserEmail then "" else account.UserEmail)
        this.HttpContext.Session.SetString("UserRole", (int account.Role).ToString())

    [<HttpGet>]
    member this.Register() : IActionResult =
        this.View(RegisterViewModel()) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Register(model: RegisterViewModel) : Task<IActionResult> =
        task {
            if model.UserPassword <> model.ConfirmPassword then
                this.ModelState.AddModelError("ConfirmPassword", "Die Passwörter stimmen nicht überein.")

            let! existing = userRepository.FindAccountByEmailAsync(if isNull model.UserEmail then "" else model.UserEmail)

            if existing.IsSome then
                this.ModelState.AddModelError("UserEmail", "Diese E-Mail ist bereits registriert.")

            if not this.ModelState.IsValid then
                return this.View(model) :> IActionResult
            else
                let account = UserAccount()
                account.UserId <- Guid.NewGuid().ToString()
                account.UserName <- model.UserName
                account.UserEmail <- model.UserEmail
                account.UserPassword <- model.UserPassword
                account.Role <- UserRoleType.User
                account.IsActivated <- false

                do! userRepository.AddNewAccountAsync(account)

                let email = if isNull account.UserEmail then "" else account.UserEmail
                let! code = emailActivationRepository.RequestActivationAsync(email)

                this.TempData.["Message"] <-
                    box "Ihr Konto wurde erfolgreich erstellt. Bitte prüfen Sie Ihre E-Mail auf den Aktivierungscode."

                if env.IsDevelopment() && not (String.IsNullOrEmpty(code)) then
                    this.TempData.["DevCode"] <- box code

                return this.RedirectToAction("Login") :> IActionResult
        }

    [<HttpGet>]
    member this.Login() : IActionResult =
        this.View(LoginViewModel()) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Login(model: LoginViewModel) : Task<IActionResult> =
        task {
            let! account = userRepository.FindAccountByEmailAsync(if isNull model.Email then "" else model.Email)

            match account with
            | Some acc when acc.UserPassword = model.Password ->
                this.SignInUser(acc)
                return this.RedirectToAction("Index", "Home") :> IActionResult
            | _ ->
                this.ModelState.AddModelError("", "Ungültige E-Mail oder ungültiges Passwort.")
                return this.View(model) :> IActionResult
        }

    member this.Logout() : IActionResult =
        this.HttpContext.Session.Clear()
        this.RedirectToAction("Index", "Home") :> IActionResult

    [<HttpGet>]
    member this.Activate() : IActionResult =
        let model = ActivateViewModel()

        match this.TempData.["Email"] with
        | null -> ()
        | value ->
            let email = Convert.ToString(value)
            if not (String.IsNullOrEmpty(email)) then model.Email <- email

        this.View(model) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Activate(model: ActivateViewModel) : Task<IActionResult> =
        task {
            let email = if isNull model.Email then "" else model.Email
            let code = if isNull model.Code then "" else model.Code
            let! ok = emailActivationRepository.ActivateAccountAsync(email, code)

            if ok then
                this.TempData.["Message"] <-
                    box "Ihr Konto wurde erfolgreich aktiviert. Sie können sich jetzt anmelden."

                return this.RedirectToAction("Login") :> IActionResult
            else
                this.ModelState.AddModelError("", "Ungültiger Aktivierungscode.")
                return this.View(model) :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.ResendCode(email: string) : Task<IActionResult> =
        task {
            let! code = emailActivationRepository.RequestActivationAsync(if isNull email then "" else email)

            if not (String.IsNullOrEmpty(code)) then
                this.TempData.["Message"] <- box "Ein neuer Aktivierungscode wurde an Ihre E-Mail gesendet."

                if env.IsDevelopment() then
                    this.TempData.["DevCode"] <- box code

                this.TempData.["Email"] <- box email
            else
                this.TempData.["Message"] <-
                    box "Falls diese E-Mail existiert, wurde ein neuer Aktivierungscode gesendet."

            return this.RedirectToAction("Activate") :> IActionResult
        }

    member this.Profile() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                let! account = userRepository.FindAccountByUserId(this.CurrentUserId())

                match account with
                | None -> return this.NotFound() :> IActionResult
                | Some acc -> return this.View(acc) :> IActionResult
        }

    [<HttpGet>]
    member this.Edit() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                let! account = userRepository.FindAccountByUserId(this.CurrentUserId())

                match account with
                | None -> return this.NotFound() :> IActionResult
                | Some acc -> return this.View(acc) :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Edit(user: UserAccount) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            elif this.ModelState.IsValid then
                do! userRepository.UpdateAccountAsync(user, this.CurrentUserId())
                this.TempData.["Message"] <- box "Ihre Kontodaten wurden erfolgreich aktualisiert."
                return this.RedirectToAction("Profile") :> IActionResult
            else
                return this.View(user) :> IActionResult
        }

    [<HttpGet>]
    member this.EditProfileImage() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                let! account = userRepository.FindAccountByUserId(this.CurrentUserId())

                match account with
                | None -> return this.NotFound() :> IActionResult
                | Some acc -> return this.View(acc) :> IActionResult
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.EditProfileImage(profileImage: IFormFile) : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                if not (isNull profileImage) && profileImage.Length > 0L then
                    let uploadsFolder = Path.Combine(env.WebRootPath, "uploads")
                    Directory.CreateDirectory(uploadsFolder) |> ignore

                    let uniqueFileName =
                        Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImage.FileName)

                    let filePath = Path.Combine(uploadsFolder, uniqueFileName)

                    use fileStream = new FileStream(filePath, FileMode.Create)
                    do! profileImage.CopyToAsync(fileStream)
                    do! userRepository.UpdateProfileImageAsync(this.CurrentUserId(), "/uploads/" + uniqueFileName)
                    this.TempData.["Message"] <- box "Profilbild wurde erfolgreich aktualisiert."

                return this.RedirectToAction("Profile") :> IActionResult
        }

    [<HttpGet>]
    member this.Delete() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                let! account = userRepository.FindAccountByUserId(this.CurrentUserId())

                match account with
                | None -> return this.NotFound() :> IActionResult
                | Some acc -> return this.View(acc) :> IActionResult
        }

    [<HttpPost>]
    [<ActionName("Delete")>]
    [<ValidateAntiForgeryToken>]
    member this.DeleteConfirmed() : Task<IActionResult> =
        task {
            if not (this.RequireLogin()) then
                return this.RedirectToAction("Login") :> IActionResult
            else
                do! userRepository.DeleteAccountAsync(this.CurrentUserId())
                this.HttpContext.Session.Clear()
                this.TempData.["Message"] <- box "Ihr Konto wurde erfolgreich gelöscht."
                return this.RedirectToAction("Index", "Home") :> IActionResult
        }
