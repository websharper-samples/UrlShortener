namespace UrlShortener

open WebSharper
open WebSharper.AspNetCore
open WebSharper.Sitelets
open WebSharper.OAuth
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server
open Microsoft.Extensions.Configuration

type MainTemplate = Templating.Template<"Main.html", serverLoad = Templating.ServerLoad.WhenChanged>

/// The full website.
type Site(config: IConfiguration) =
    inherit ISiteletService<EndPoint>()

    let NavBar (ctx: Context<EndPoint>) =
        MainTemplate.MainNavBar()
            .HomeUrl(ctx.Link Home)
            .MyLinksUrl(ctx.Link MyLinks)
            .LogoutUrl(ctx.Link Logout)
            .Doc()

    /// The page layout used by all our HTML content.
    let Page (ctx: Context<EndPoint>) (withNavBar: bool) (body: Doc) =
        Content.Page(
            MainTemplate()
                .Body(body)
                .NavBar(if withNavBar then NavBar ctx else Doc.Empty)
                .Doc()
        )

    /// Content for the login page.
    let LoginPage (ctx: Context<EndPoint>) (facebook: OAuth2.Provider<EndPoint>) =
        MainTemplate.LoginPage()
            .FacebookLoginUrl(facebook.GetAuthorizationRequestUrl(ctx))
            .Doc()
        |> Page ctx false

    /// Content for the home page once logged in.
    let HomePage (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.HomePage()
            .Doc()
        |> Page ctx true

    /// Content for the account management page.
    let MyLinksPage (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.MyLinksPage()
            .FullName(name)
            .Doc()
        |> Page ctx true

    /// Content for an actual redirection link.
    let LinkContent ctx slug =
        // TODO: get the URL from the database
        Content.RedirectPermanentToUrl "https://websharper.com"
        
    let facebook = Authentication.FacebookProvider config
    
    override val Sitelet =
        facebook.RedirectEndpointSitelet
        <|>
        Application.MultiPage (fun (ctx: Context<EndPoint>) endpoint -> async {
            match endpoint with
            | Home ->
                match! Authentication.GetLoggedInUserData ctx with
                | None -> return! LoginPage ctx facebook
                | Some name -> return! HomePage ctx name
            | MyLinks ->
                match! Authentication.GetLoggedInUserData ctx with
                | None -> return! Content.RedirectTemporary Home
                | Some name -> return! MyLinksPage ctx name
            | Link slug ->
                return! LinkContent ctx slug
            | Logout ->
                do! ctx.UserSession.Logout()
                return! Content.RedirectTemporary Home
            | OAuth ->
                return! Content.ServerError
        })
