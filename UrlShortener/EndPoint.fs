namespace UrlShortener

open WebSharper

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/my-links">] MyLinks
    | [<EndPoint "/">] Link of slug: string
    | [<EndPoint "/oauth">] OAuth
