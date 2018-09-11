namespace UrlShortener

open System
open WebSharper
open UrlShortener.DataModel
open UrlShortener.Database

module Remoting =

    let private getAllUserLinks (ctx: Web.Context) (userId: Guid) =
        async {
            let! links = ctx.Db.GetAllUserLinks userId
            return links
                |> Array.map (fun l ->
                    let slug = EncodeLinkId l.Id
                    let url = SlugToFullUrl ctx slug
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

    [<Remote>]
    let CreateNewLink (url: string) =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                let! _ = ctx.Db.CreateLink(userId, url)
                return! getAllUserLinks ctx userId
        }
