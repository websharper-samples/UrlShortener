/// Core data model types and functionality.
module UrlShortener.DataModel

open System
open WebSharper
open WebSharper.Sitelets.InferRouter

/// Defines all the endpoints served by this application.
type EndPoint =
    | [<EndPoint "GET /">] Home
    | [<EndPoint "GET /my-links">] MyLinks
    | [<EndPoint "GET /logout">] Logout
    | [<EndPoint "GET /oauth">] OAuth
    | [<EndPoint "GET /">] Link of slug: string
    | [<EndPoint "POST /create-link"; FormData "url">] CreateLink of url: string

/// Enables creating URLs from EndPoints and vice-versa.
let Router = Router.Infer<EndPoint>()

/// Functionality regarding redirection links.
module Link =

    /// Link id as stored in the database.
    type Id = int64

    /// Link id as shown in URLs.
    type Slug = string

    /// Information about a redirection link.
    [<JavaScript>]
    type Data =
        {
            Slug: Slug
            LinkUrl: string
            TargetUrl: string
            VisitCount: int64
        }

    /// Create a link slug from a link id.
    /// Uses base64-URL encoding.
    let EncodeLinkId (linkId: Id) : Slug =
        let bytes = BitConverter.GetBytes(linkId)
        Convert.ToBase64String(bytes)
            .Replace("=", "")
            .Replace('+', '-')
            .Replace('/', '_')

    /// Extract the link id from a link slug.
    /// Uses base64-URL decoding.
    let TryDecodeLinkId (slug: Slug) : Id option =
        if String.IsNullOrEmpty slug then
            Some 0L
        else
            let s =
                slug.Replace('-', '+')
                    .Replace('_', '/')
                + (String.replicate (4 - slug.Length % 4) "=")
            try BitConverter.ToInt64(Convert.FromBase64String s, 0) |> Some
            with _ -> None

    /// Create a new random link id.
    let NewLinkId() : Id =
        let bytes = Array.zeroCreate<byte> 8
        Random().NextBytes(bytes)
        BitConverter.ToInt64(bytes, 0)

    /// Create a full URL from a link slug.
    let SlugToFullUrl (ctx: Web.Context) (slug: Slug) : string =
        let builder = UriBuilder(ctx.RequestUri)
        builder.Path <- Router.Link(Link slug)
        builder.Uri.ToString()

/// Functionality regarding application users.
module User =

    /// User ids are GUIDs both in the database and in user code.
    type Id = Guid

    /// Information about a user.
    [<JavaScript>]
    type Data =
        {
            UserId: Id
            FullName: string
        }

    /// Create a new random user id.
    let NewUserId() : Id =
        Guid.NewGuid()
