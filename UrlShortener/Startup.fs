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
        services.AddAuthentication("WebSharper")
            .AddCookie("WebSharper", fun options -> ())
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        Database.Migrate config logger

        app.UseAuthentication()
            .UseStaticFiles()
            .UseWebSharper(env, Site.Main config, config.GetSection("websharper"))
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Page not found"))

module Program =

    [<EntryPoint>]
    let main args =
        WebHost
            .CreateDefaultBuilder(args)
            .ConfigureLogging(fun ctx logging ->
                logging
                    .AddConfiguration(ctx.Configuration.GetSection("Logging"))
                    .AddConsole()
                    .AddDebug()
                |> ignore)
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
