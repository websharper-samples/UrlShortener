[<WebSharper.JavaScript>]
module UrlShortener.Client

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Mvu
open WebSharper.Mvu.Action

module Model =

    type LinkModel =
        {
            LinkUrl: string
            TargetUrl: string
            VisitCount: int
            Deleting: bool
        }

    type Model =
        {
            UserDisplayName: string
            Links: Map<string, LinkModel>
            IsLoading: bool
        }

    let InitialModel userDisplayName =
        {
            UserDisplayName = userDisplayName
            Links = Map.empty
            IsLoading = true
        }

    type Message =
        | Refresh
        | Refreshed of links: Remoting.LinkData[]
        | Delete of slug: string

module View =
    open Model
    
    let UpdateButton dispatch (model: View<Model>) =
        button [
            on.click (fun _ _ -> dispatch Refresh)
            attr.``class`` "button is-info"
            Attr.ClassPred "is-loading" model.V.IsLoading
        ] [
            text "Update list"
        ]
    
    let DeleteButton dispatch slug (link: View<LinkModel>) =
        button [
            on.click (fun _ _ -> dispatch (Delete slug))
            attr.``class`` "button is-info"
            Attr.ClassPred "is-loading" link.V.Deleting
        ] [
            text "Delete"
        ]

    let UrlLink (link: View<string>) =
        a [
            attr.href link.V
            attr.target "_blank"
        ] [
            text link.V
        ]

    let Page dispatch (model: View<Model>) =
        let total =
            V(Map.fold (fun res _ v -> res + v.VisitCount) 0 model.V.Links)
        Doc.Concat [
            h1 [attr.``class`` "title"] [
                text ("Welcome, " + model.V.UserDisplayName + "!")
            ]
            p [attr.``class`` "subtitle"] [
                text "Here are the links you have created:"
            ]
            table [attr.``class`` "table is-fullwidth"] [
                thead [] [
                    tr [] [
                        th [] [text "Link"]
                        th [] [text "Target"]
                        th [] [text "Visits"]
                        th [] [UpdateButton dispatch model]
                    ]
                ]
                tbody [] [
                    (V model.V.Links).DocSeqCached(fun slug link ->
                        tr [] [
                            td [] [UrlLink (V link.V.LinkUrl)]
                            td [] [UrlLink (V link.V.TargetUrl)]
                            td [] [text (string link.V.VisitCount)]
                            td [] [DeleteButton dispatch slug link]
                        ]
                    )
                ]
                tfoot [] [
                    tr [] [
                        th [] []
                        th [attr.``class`` "has-text-right"] [text "Total:"]
                        th [] [text (string total.V)]
                        th [] []
                    ]
                ]
            ]
        ]

module Update =
    open Model

    // /// This is an artificially slowed-down version of DispatchAsync
    // /// so that we can see the loading buttons even though
    // /// local remote calls run pretty much instantly.
    // let DispatchAsync msg call =
    //     DispatchAsync msg (async {
    //         let! res = call
    //         do! Async.Sleep 1000
    //         return res
    //     })

    let Update (message: Message) (model: Model) =
        match message with
        | Refresh ->
            SetModel { model with IsLoading = true }
            +
            DispatchAsync Refreshed (Remoting.GetMyLinks ())
        | Delete slug ->
            DispatchAsync Refreshed (Remoting.DeleteLink slug)
        | Refreshed links ->
            let links =
                links
                |> Array.map (fun link ->
                    link.Slug, {
                        LinkUrl = link.LinkUrl
                        TargetUrl = link.TargetUrl
                        VisitCount = link.VisitCount
                        Deleting = false
                    }
                )
                |> Map.ofArray
            SetModel { model with Links = links; IsLoading = false }

open Model
open View
open Update

let MyLinks (userDisplayName: string) =
    App.Create (InitialModel userDisplayName) Update Page
    |> App.WithInitAction
        (Action.Command (fun dispatch -> dispatch Refresh))
    |> App.Run
