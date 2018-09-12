namespace UrlShortener

open System
open WebSharper
open UrlShortener.DataModel
open UrlShortener.Database

/// Functions that can be called from the client side.
module Remoting =

    /// Retrieve a list of the links created by the logged in user.
    [<Remote>]
    let GetMyLinks () : Async<Link.Data[]> =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId -> return! ctx.Db.GetAllUserLinks(userId, ctx)
        }

    /// Delete this link, if it was created by the logged in user.
    /// Return a list of the links created by the logged in user.
    [<Remote>]
    let DeleteLink (slug: Link.Slug) : Async<Link.Data[]> =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                do! ctx.Db.DeleteLink(userId, slug)
                return! ctx.Db.GetAllUserLinks(userId, ctx)
        }

    /// Create a new link pointing to this URL.
    /// Return a list of the links created by the logged in user.
    [<Remote>]
    let CreateNewLink (url: Link.Slug) : Async<Link.Data[]> =
        let ctx = Web.Remoting.GetContext()
        async {
            match! Authentication.GetLoggedInUserId ctx with
            | None -> return Array.empty
            | Some userId ->
                let! _ = ctx.Db.CreateLink(userId, url)
                return! ctx.Db.GetAllUserLinks(userId, ctx)
        }
