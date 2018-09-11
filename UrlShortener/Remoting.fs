namespace UrlShortener

open System
open WebSharper
open WebSharper.Sitelets.InferRouter
open UrlShortener.DataModel
open UrlShortener.Database

module Remoting =

    let private router = Router.Infer<EndPoint>()

    let private getAllUserLinks (ctx: Web.Context) (userId: Guid) =
        async {
            let! links = ctx.Db.GetAllUserLinks userId
            return links
                |> Array.map (fun l ->
                    let slug = EncodeLinkId l.Id
                    let path = router.Link(Link slug)
                    let url =
                        UriBuilder(ctx.RequestUri, Path = path)
                            .ToString()
                    {
                        Slug = slug
                        LinkUrl = url
                        TargetUrl = l.Url
                        VisitCount = l.VisitCount
                    } : DataModel.LinkData
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
