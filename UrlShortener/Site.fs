namespace UrlShortener

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

    let HomeContent ctx =
        MainTemplate.LoginPage()
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
                let! loggedIn = ctx.UserSession.GetLoggedInUser()
                match loggedIn with
                | None -> return! LoginContent ctx facebook
                | Some uid -> return! HomeContent ctx
            | MyLinks ->
                let! loggedIn = ctx.UserSession.GetLoggedInUser()
                match loggedIn with
                | None -> return! Content.RedirectTemporary Home
                | Some uid -> return! MyLinksContent ctx
            | Link slug -> return! LinkContent ctx slug
            | OAuth -> return! Content.ServerError
        })
