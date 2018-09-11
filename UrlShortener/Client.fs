[<WebSharper.JavaScript>]
module UrlShortener.Client

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Mvu

module Model =

    type LinkModel =
        {
            LinkUrl: string
            TargetUrl: string
            VisitCount: int64
            Deleting: bool
        }

    type Model =
        {
            UserDisplayName: string
            Links: Map<string, LinkModel>
            IsLoading: bool
            NewLinkText: string
        }

    let InitialModel userDisplayName =
        {
            UserDisplayName = userDisplayName
            Links = Map.empty
            IsLoading = true
            NewLinkText = ""
        }

    type Message =
        | Refresh
        | Refreshed of links: DataModel.LinkData[]
        | Delete of slug: string
        | SetNewLinkText of string
        | CreateNewLink

module View =
    open Model

    let NewLinkForm dispatch (model: View<Model>) =
        form [
            on.submit (fun _ ev ->
                ev.PreventDefault()
                dispatch CreateNewLink)
            attr.``class`` "field has-addons has-addons-centered"
        ] [
            div [attr.``class`` "control"] [
                input [
                    attr.``class`` "input is-large"
                    attr.placeholder "Shorten this link"
                    attr.value model.V.NewLinkText
                    on.change (fun el _ -> dispatch (SetNewLinkText el?value))
                ] []
            ]
            div [attr.``class`` "control"] [
                button [
                    attr.``class`` "button is-info is-large"
                    Attr.ClassPred "is-loading" model.V.IsLoading
                ] [
                    text "Shorten"
                ]
            ]
        ]
    
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
            V(Map.fold (fun res _ v -> res + v.VisitCount) 0L model.V.Links)
        Doc.Concat [
            h1 [attr.``class`` "title has-text-centered"] [
                text ("Welcome, " + model.V.UserDisplayName + "!")
            ]
            NewLinkForm dispatch model
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
        | SetNewLinkText url ->
            SetModel { model with NewLinkText = url }
        | CreateNewLink ->
            SetModel { model with IsLoading = true; NewLinkText = "" }
            +
            DispatchAsync Refreshed (Remoting.CreateNewLink model.NewLinkText)

open Model
open View
open Update

let MyLinks (userDisplayName: string) =
    App.Create (InitialModel userDisplayName) Update Page
    |> App.WithInitAction
        (Action.Command (fun dispatch -> dispatch Refresh))
    |> App.Run
