namespace UrlShortener

open System
open WebSharper
open UrlShortener.Database

module Remoting =

    [<JavaScript>]
    type LinkData =
        {
            Slug: string
            LinkUrl: string
            TargetUrl: string
            VisitCount: int64
        }

    let private getAllUserLinks (ctx: Web.Context) (userId: Guid) =
        async {
            let! links = ctx.Db.GetAllUserLinks userId
            return links
                |> Array.map (fun l ->
                    let slug = string l.Id
                    let path = "/" + slug
                    let url =
                        UriBuilder(ctx.RequestUri, Path = path)
                            .ToString()
                    {
                        Slug = slug
                        LinkUrl = url
                        TargetUrl = l.Url
                        VisitCount = l.VisitCount
                    }
                )
        }

    [<Remote>]
    let GetMyLinks () =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId -> return! getAllUserLinks ctx userId
        }

    [<Remote>]
    let DeleteLink (slug: string) =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                do! ctx.Db.DeleteLink(userId, slug)
                return! getAllUserLinks ctx userId
        }
