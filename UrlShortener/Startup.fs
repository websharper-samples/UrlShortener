namespace UrlShortener

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open WebSharper.AspNetCore

type Startup(loggerFactory: ILoggerFactory, config: IConfiguration) =
    let logger = loggerFactory.CreateLogger<Startup>()

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddSitelet<Site>()
                .AddScoped<Database.Context>()
                .AddAuthentication("WebSharper")
                .AddCookie("WebSharper", fun options -> ())
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment, db: Database.Context) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        db.Migrate()

        app.UseAuthentication()
            .UseStaticFiles()
            .UseWebSharper()
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Page not found"))

module Program =

    [<EntryPoint>]
    let main args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
