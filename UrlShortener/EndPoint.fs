namespace UrlShortener

open WebSharper

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/my-links">] MyLinks
    | [<EndPoint "/logout">] Logout
    | [<EndPoint "/oauth">] OAuth
    | [<EndPoint "/">] Link of slug: string
