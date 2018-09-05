namespace UrlShortener

open WebSharper
open WebSharper.AspNetCore
open WebSharper.Sitelets
open WebSharper.OAuth
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server
open Microsoft.Extensions.Configuration

type MainTemplate = Templating.Template<"Main.html", serverLoad = Templating.ServerLoad.PerRequest>

/// The full website.
type Site(config: IConfiguration) =
    inherit ISiteletService<EndPoint>()

    /// The main page layout used by all our HTML content.
    let MainPage (body: Doc) =
        Content.Page(
            MainTemplate()
                .Body(body)
                .Doc()
        )

    /// Content for the login page.
    let LoginContent (ctx: Context<EndPoint>) (facebook: OAuth2.Provider<EndPoint>) =
        MainTemplate.LoginPage()
            .FacebookLoginUrl(facebook.GetAuthorizationRequestUrl(ctx))
            .Doc()
        |> MainPage

    /// Content for the home page once logged in.
    let HomeContent (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.HomePage()
            .FullName(name)
            .LogoutUrl(ctx.Link Logout)
            .Doc()
        |> MainPage

    /// Content for the account management page.
    let MyLinksContent ctx =
        text "TODO"
        |> MainPage

    /// Content for an actual redirection link.
    let LinkContent ctx slug =
        text ("TODO: redirect to " + slug)
        |> MainPage
        
    let facebook = Authentication.FacebookProvider config
    
    override val Sitelet =
        facebook.RedirectEndpointSitelet
        <|>
        Application.MultiPage (fun (ctx: Context<EndPoint>) endpoint -> async {
            match endpoint with
            | Home ->
                match! Authentication.GetLoggedInUserData ctx with
                | None -> return! LoginContent ctx facebook
                | Some name -> return! HomeContent ctx name
            | MyLinks ->
                match! Authentication.GetLoggedInUserData ctx with
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
