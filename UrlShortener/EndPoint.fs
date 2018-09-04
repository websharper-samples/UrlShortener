namespace UrlShortener

open WebSharper

/// Defines all the endpoints served by this application.
type EndPoint =
    | [<EndPoint "GET /">] Home
    | [<EndPoint "GET /my-links">] MyLinks
    | [<EndPoint "GET /logout">] Logout
    | [<EndPoint "GET /oauth">] OAuth
    | [<EndPoint "GET /">] Link of slug: string
