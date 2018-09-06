module UrlShortener.Authentication

open System
open System.IO
open System.Net
open WebSharper
open WebSharper.Sitelets
open WebSharper.OAuth.OAuth2
open Microsoft.Extensions.Configuration
open UrlShortener.Database

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

/// The OAuth2 client for Facebook login.
let FacebookProvider (config: IConfiguration) =
    let config = config.GetSection("facebook")
    Provider.Setup(
        // Create a Facebook OAuth client with the given app credentials.
        service = ServiceSettings.Facebook(config.["clientId"], config.["clientSecret"]),
        // Upon success or failure, users are redirected to EndPoint.OAuth (which points to to "/oauth").
        redirectEndpointAction = OAuth,
        redirectEndpoint = (fun ctx resp ->
            match resp with
            | AuthenticationResponse.Success token ->
                // On successful Facebook login:
                async {
                    // 1. Query Facebook for user data (id, full name);
                    let! fbUserData = getFacebookUserData token
                    // 2. Match it with a user in our database, or create one;
                    let! userId = ctx.Db.GetOrCreateFacebookUser(fbUserData.id, fbUserData.name)
                    // 3. Log the user in;
                    do! ctx.UserSession.LoginUser(userId.ToString("N"))
                    // 4. Redirect them to the home page.
                    return! Content.RedirectTemporary Home
                }
            | AuthenticationResponse.Error e ->
                // On failure, show an error message.
                e.Description
                |> Option.defaultValue "Failed to log in with Facebook"
                |> Content.Text
                |> Content.SetStatus Http.Status.InternalServerError
            | AuthenticationResponse.ImplicitSuccess ->
                // Implicit login is used for client-only applications, we don't use it.
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
let GetLoggedInUserData (ctx: Web.Context) = async {
    match! GetLoggedInUserId ctx with
    | None -> return None
    | Some uid -> return! ctx.Db.GetFullName(uid)
}