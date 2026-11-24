namespace PackstationFsharp.Models

open System.Threading.Tasks

type IEmailSender =
    abstract SendEmailAsync : toEmail: string * subject: string * body: string -> Task<unit>
