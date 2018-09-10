namespace UrlShortener

open System
open WebSharper
open WebSharper.AspNetCore
open WebSharper.Sitelets
open WebSharper.OAuth
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server
open Microsoft.Extensions.Configuration
open UrlShortener.Database

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

    /// The page body layout for "splash" style pages: home, login, link created.
    let SplashBody (title: string) (subtitle: option<string>) (content: Doc) =
        MainTemplate.SplashPage()
            .Title(title)
            .Subtitle(defaultArg subtitle "")
            .Content(content)
            .Doc()

    /// Content for the login page.
    let LoginPage (ctx: Context<EndPoint>) (facebook: OAuth2.Provider<EndPoint>) =
        a [
            attr.href (facebook.GetAuthorizationRequestUrl(ctx))
            attr.``class`` "button is-block is-info is-large is-fullwidth"
        ] [
            text "Log in with Facebook"
        ]
        |> SplashBody "WebSharper URL Shortener" (Some "Please login to proceed.")
        |> Page ctx false

    /// Content for the home page once logged in.
    let HomePage (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.HomeContent()
            .CreateLinkUrl(ctx.Link (CreateLink ""))
            .Doc()
        |> SplashBody "WebSharper URL Shortener" None
        |> Page ctx true

    /// Content for the account management page.
    let MyLinksPage (ctx: Context<EndPoint>) (name: string) =
        MainTemplate.MyLinksPage()
            .Content(client <@ Client.MyLinks name @>)
            .Doc()
        |> Page ctx true

    /// Content shown after successfully creating a link.
    let LinkCreatedPage (ctx: Context<EndPoint>) (linkId: string) =
        let path = ctx.Link (Link linkId)
        let url = UriBuilder(ctx.RequestUri, Path = path).ToString()
        div [attr.``class`` "title"] [
            a [
                attr.href url
                attr.target "_blank"
                attr.``class`` "button is-large is-success"
            ] [
                text url
            ]
        ]
        |> SplashBody "Link created!" None
        |> Page ctx true

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
                match! ctx.Db.TryGetLink(slug) with
                | Some url -> return! Content.RedirectTemporaryToUrl url
                | None -> return! Content.NotFound // TODO: put some HTML content on the 404 page
            | Logout ->
                do! ctx.UserSession.Logout()
                return! Content.RedirectTemporary Home
            | CreateLink url ->
                match! Authentication.GetLoggedInUserId ctx with
                | None -> return! Content.RedirectTemporary Home
                | Some uid ->
                    let! slug = ctx.Db.CreateLink(uid, url)
                    return! LinkCreatedPage ctx slug
            | OAuth ->
                return! Content.ServerError
        })
