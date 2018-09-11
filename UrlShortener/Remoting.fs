namespace UrlShortener

open System
open WebSharper
open UrlShortener.DataModel
open UrlShortener.Database

module Remoting =

    [<Remote>]
    let GetMyLinks () =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId -> return! ctx.Db.GetAllUserLinks(userId, ctx)
        }

    [<Remote>]
    let DeleteLink (slug: string) =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                do! ctx.Db.DeleteLink(userId, slug)
                return! ctx.Db.GetAllUserLinks(userId, ctx)
        }

    [<Remote>]
    let CreateNewLink (url: string) =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                let! _ = ctx.Db.CreateLink(userId, url)
                return! ctx.Db.GetAllUserLinks(userId, ctx)
        }
