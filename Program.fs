module Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open PackstationFsharp.Models
open PackstationFsharp.Repositories

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Add services to the container.
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation() |> ignore

    builder.Services.AddDistributedMemoryCache() |> ignore
    builder.Services.AddSession() |> ignore

    builder.Services.AddScoped<DbManageConnection>(
        Func<IServiceProvider, DbManageConnection>(fun _ ->
            new DbManageConnection(ConnectionMode.GetRepository, builder.Configuration)
        )
    )
    |> ignore

    builder.Services.AddScoped<IUserRepository, UserRepository>() |> ignore
    builder.Services.AddScoped<IEmailSender, EmailSender>() |> ignore
    builder.Services.AddScoped<IEmailActivationRepository, EmailActivationRepository>() |> ignore
    builder.Services.AddScoped<IParcelRepository, ParcelRepository>() |> ignore
    builder.Services.AddScoped<ILockerRepository, LockerRepository>() |> ignore

    let app = builder.Build()

    // Initialize database
    try
        use adminConn = new DbManageConnection(ConnectionMode.CreateDatabase, app.Configuration)
        let dbBuilder = DbBuilder(adminConn)
        dbBuilder.GetCreateDatabaseAsync().GetAwaiter().GetResult()

        use repoConn = new DbManageConnection(ConnectionMode.GetRepository, app.Configuration)
        let tableBuilder = DbBuilder(repoConn)
        tableBuilder.GetCreateTableAsync().GetAwaiter().GetResult()

        app.Logger.LogInformation("Database and tables are ready.")
    with ex ->
        app.Logger.LogError(ex, "Database initialization failed: {Message}", ex.Message)

    // Configure the HTTP request pipeline.
    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Home/Error") |> ignore
        app.UseHsts() |> ignore

    app.UseHttpsRedirection() |> ignore
    app.UseStaticFiles() |> ignore

    app.UseRouting() |> ignore
    app.UseSession() |> ignore

    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}") |> ignore

    app.Run()
    0
