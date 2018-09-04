[<System.Runtime.CompilerServices.Extension>]
module UrlShortener.Authentication

open System
open System.Net
open WebSharper
open WebSharper.Sitelets
open WebSharper.OAuth.OAuth2
open Microsoft.Extensions.Configuration
open System.IO

/// The JSON returned by Facebook when querying user data.
type private FacebookUserData = { id: string; name: string }

/// Get the user data for the owner of this token.
let private getFacebookUserData (token: AuthenticationToken) : Async<FacebookUserData> =
    async {
        let req = WebRequest.CreateHttp("https://graph.facebook.com/v3.1/me")
        token.AuthorizeRequest req
        let! resp = req.AsyncGetResponse()
        use r = new StreamReader(resp.GetResponseStream())
        let! body = r.ReadToEndAsync() |> Async.AwaitTask
        return WebSharper.Json.Deserialize(body)
    }

/// The OAuth2 provider for Facebook login.
let FacebookProvider (config: IConfiguration) =
    let clientId = config.["facebookClientId"]
    let clientSecret = config.["facebookClientSecret"]
    Provider.Setup(
        service = ServiceSettings.Facebook(clientId, clientSecret),
        redirectEndpointAction = OAuth,
        redirectEndpoint = (fun ctx resp ->
            match resp with
            | AuthenticationResponse.Success token ->
                async {
                    let! fbUserData = getFacebookUserData token
                    let db = Database.GetDataContext config
                    let! userId = Database.GetOrCreateFacebookUser db fbUserData.id fbUserData.name
                    do! ctx.UserSession.LoginUser(userId.ToString("N"))
                    return! Content.RedirectTemporary Home
                }
            | AuthenticationResponse.Error e ->
                e.Description
                |> Option.defaultValue "Failed to log in with Facebook"
                |> Content.Text
                |> Content.SetStatus Http.Status.InternalServerError
            | AuthenticationResponse.ImplicitSuccess ->
                Content.Text "This application doesn't use implicit login"
                |> Content.SetStatus Http.Status.NotImplemented
        )
    )

/// Get the user id of the currently logged in user.
let GetLoggedInUserId (ctx: Web.Context) = async {
    match! ctx.UserSession.GetLoggedInUser() with
    | None -> return None
    | Some s ->
        match Guid.TryParse(s) with
        | true, id -> return Some id
        | false, _ ->
            do! ctx.UserSession.Logout()
            return None
}

/// Get the user data of the currently logged in user.
let GetLoggedInUserData (ctx: Web.Context) (config: IConfiguration) = async {
    match! GetLoggedInUserId ctx with
    | None -> return None
    | Some uid ->
        let db = Database.GetDataContext config
        return! Database.GetFullName db uid
}