module UrlShortener.Authentication

open WebSharper.Sitelets
open WebSharper.OAuth.OAuth2
open Microsoft.Extensions.Configuration

let FacebookProvider (config: IConfiguration) =
    let clientId = config.["facebookClientId"]
    let clientSecret = config.["facebookClientSecret"]
    Provider.Setup(
        service = ServiceSettings.Facebook(clientId, clientSecret),
        redirectEndpointAction = OAuth,
        redirectEndpoint = (fun ctx resp ->
            match resp with
            | AuthenticationResponse.Success token ->
                // TODO: check db and log in user
                Content.RedirectTemporary Home
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
