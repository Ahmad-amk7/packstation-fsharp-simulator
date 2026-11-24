namespace PackstationFsharp.Models

open System
open System.Threading.Tasks
open MailKit.Net.Smtp
open Microsoft.Extensions.Configuration
open MimeKit
open PackstationFsharp

type EmailSender(configuration: IConfiguration) =

    interface IEmailSender with
        member _.SendEmailAsync(toEmail: string, subject: string, body: string) =
            task {
                try
                    let emailSettings = configuration.GetSection("EmailSettings")
                    let message = MimeMessage()
                    message.From.Add(MailboxAddress(emailSettings.["FromName"], emailSettings.["FromAddress"]))
                    message.To.Add(MailboxAddress(toEmail, toEmail))
                    message.Subject <- subject
                    message.Body <- TextPart("html", Text = body)

                    use client = new SmtpClient()
                    do!
                        client.ConnectAsync(
                            emailSettings.["SmtpHost"],
                            int (emailSettings.["SmtpPort"]),
                            MailKit.Security.SecureSocketOptions.StartTls
                        )
                    do! client.AuthenticateAsync(emailSettings.["SmtpUser"], emailSettings.["SmtpPassword"])
                    do! DbHelpers.ignoreTask (client.SendAsync(message))
                    do! client.DisconnectAsync(true)
                with ex ->
                    Console.WriteLine($"Email sending failed: {ex.Message}")
            }
