namespace UrlShortener

open System
open WebSharper
open WebSharper.Sitelets
open WebSharper.OAuth
open WebSharper.UI
open WebSharper.UI.Server

module Site =
    open WebSharper.UI.Html

    type MainTemplate = Templating.Template<"Main.html", serverLoad = Templating.ServerLoad.PerRequest>

    let MainPage (body: Doc) =
        Content.Page(
            MainTemplate()
                .Body(body)
                .Doc()
        )

    let LoginContent (ctx: Context<EndPoint>) (facebook: OAuth2.Provider<EndPoint>) =
        MainTemplate.LoginPage()
            .FacebookLoginUrl(facebook.GetAuthorizationRequestUrl(ctx))
            .Doc()
        |> MainPage

    let HomeContent (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.HomePage()
            .FullName(name)
            .LogoutUrl(ctx.Link Logout)
            .Doc()
        |> MainPage

    let MyLinksContent ctx =
        text "TODO"
        |> MainPage

    let LinkContent ctx slug =
        text ("TODO: redirect to " + slug)
        |> MainPage

    [<Website>]
    let Main config =
        let facebook = Authentication.FacebookProvider config

        facebook.RedirectEndpointSitelet
        <|>
        Application.MultiPage (fun (ctx: Context<EndPoint>) endpoint -> async {
            match endpoint with
            | Home ->
                match! Authentication.GetLoggedInUserData ctx config with
                | None -> return! LoginContent ctx facebook
                | Some name -> return! HomeContent ctx name
            | MyLinks ->
                match! Authentication.GetLoggedInUserData ctx config with
                | None -> return! Content.RedirectTemporary Home
                | Some name -> return! MyLinksContent ctx
            | Link slug ->
                return! LinkContent ctx slug
            | Logout ->
                do! ctx.UserSession.Logout()
                return! Content.RedirectTemporary Home
            | OAuth ->
                return! Content.ServerError
        })
