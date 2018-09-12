[<WebSharper.JavaScript>]
module UrlShortener.Client

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Mvu

/// Defines the model of this client side page,
/// ie. the data types that represent the state and its transitions.
module Model =

    /// The state of a link.
    type LinkState =
        {
            LinkUrl: string
            TargetUrl: string
            VisitCount: int64
            Deleting: bool
        }

    /// The state of the full page.
    type State =
        {
            UserDisplayName: string
            Links: Map<string, LinkState>
            IsLoading: bool
            NewLinkText: string
            PromptingDelete: option<string>
        }

        member this.TotalVisitCount =
            Map.fold (fun count _ link -> count + link.VisitCount)
                0L this.Links

    /// The initial state of the page.
    let InitialState userDisplayName =
        {
            UserDisplayName = userDisplayName
            Links = Map.empty
            IsLoading = true
            NewLinkText = ""
            PromptingDelete = None
        }

    /// The full list of actions that can be performed on the state.
    type Message =
        | Refresh
        | Refreshed of links: DataModel.LinkData[]
        | PromptDelete of slug: option<string>
        | ConfirmDelete of slug: string
        | SetNewLinkText of string
        | CreateNewLink

/// Defines the view of the page, ie how the state is rendered to HTML.
module View =
    open Model

    /// The view for the "Shorten this link" form.
    let NewLinkForm dispatch (state: View<State>) =
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
                    attr.value state.V.NewLinkText
                    on.change (fun el _ -> dispatch (SetNewLinkText el?value))
                ] []
            ]
            div [attr.``class`` "control"] [
                button [
                    attr.``class`` "button is-info is-large"
                    Attr.ClassPred "is-loading" state.V.IsLoading
                ] [
                    text "Shorten"
                ]
            ]
        ]
    
    /// The view for the "Update list" button.
    let UpdateButton dispatch (state: View<State>) =
        button [
            on.click (fun _ _ -> dispatch Refresh)
            attr.``class`` "button is-info"
            Attr.ClassPred "is-loading" state.V.IsLoading
        ] [
            text "Update list"
        ]
    
    /// The view for the "Delete" button for a given link.
    let DeleteButton dispatch slug (link: View<LinkState>) =
        button [
            on.click (fun _ _ -> dispatch (PromptDelete (Some slug)))
            attr.``class`` "button is-info"
            Attr.ClassPred "is-loading" link.V.Deleting
        ] [
            text "Delete"
        ]

    /// A link whose text is the URL itself.
    let UrlLink (link: View<string>) =
        a [attr.href link.V; attr.target "_blank"] [text link.V]

    /// The row for a link in the table.
    let LinkRow dispatch slug (link: View<LinkState>) =
        tr [] [
            td [] [UrlLink (V link.V.LinkUrl)]
            td [] [UrlLink (V link.V.TargetUrl)]
            td [] [text (string link.V.VisitCount)]
            td [] [DeleteButton dispatch slug link]
        ]

    /// A row that indicates that there are no links, if that's the case.
    let NoLinks (state: View<State>) =
        V(
            if Map.isEmpty state.V.Links then
                tr [] [
                    td [
                        attr.``class`` "has-text-centered"
                        attr.colspan "4"
                    ] [
                        text "You haven't created any links yet."
                    ]
                ]
            else
                Doc.Empty)
        |> Doc.EmbedView

    /// The view for the table of links.
    let LinksTable dispatch (state: View<State>) =
        table [attr.``class`` "table is-fullwidth"] [
            thead [] [
                tr [] [
                    th [] [text "Link"]
                    th [] [text "Target"]
                    th [] [text "Visits"]
                    th [] [UpdateButton dispatch state]
                ]
            ]
            tbody [] [
                (V state.V.Links).DocSeqCached(LinkRow dispatch)
                NoLinks state
            ]
            tfoot [] [
                tr [] [
                    th [] []
                    th [attr.``class`` "has-text-right"] [text "Total:"]
                    th [] [text (string state.V.TotalVisitCount)]
                    th [] []
                ]
            ]
        ]

    /// The modal that prompts for deleting a link.
    let ModalDelete dispatch (state: View<State>) =
        div [
            attr.``class`` "modal"
            Attr.ClassPred "is-active" state.V.PromptingDelete.IsSome
        ] [
            div [attr.``class`` "modal-background"] []
            div [attr.``class`` "modal-content box"] [
                p [attr.``class`` "subtitle" ] [
                    text "Do you really want to delete this link?"
                ]
                div [attr.``class`` "buttons is-right"] [
                    a [
                        attr.``class`` "button is-danger"
                        on.clickView state (fun _ _ v ->
                            dispatch (ConfirmDelete v.PromptingDelete.Value))
                    ] [
                        text "Yes"
                    ]
                    a [
                        attr.``class`` "button"
                        on.click (fun _ _ -> dispatch (PromptDelete None))
                    ] [
                        text "No"
                    ]
                ]
            ]
        ]

    /// The view for the full page content.
    let Page dispatch (state: View<State>) =
        Doc.Concat [
            h1 [attr.``class`` "title has-text-centered"] [
                text ("Welcome, " + state.V.UserDisplayName + "!")
            ]
            NewLinkForm dispatch state
            p [attr.``class`` "subtitle"] [
                text "Here are the links you have created:"
            ]
            LinksTable dispatch state
            ModalDelete dispatch state
        ]

/// Defines what must happen (eg. change the state, start async tasks)
/// when a message is dispatched.
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

    let Update (message: Message) (state: State) =
        match message with
        | Refresh ->
            SetModel { state with IsLoading = true }
            +
            DispatchAsync Refreshed (Remoting.GetMyLinks ())
        | PromptDelete slug ->
            SetModel { state with PromptingDelete = slug }
        | ConfirmDelete slug ->
            SetModel { state with PromptingDelete = None }
            +
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
            SetModel { state with Links = links; IsLoading = false }
        | SetNewLinkText url ->
            SetModel { state with NewLinkText = url }
        | CreateNewLink ->
            SetModel { state with IsLoading = true; NewLinkText = "" }
            +
            DispatchAsync Refreshed (Remoting.CreateNewLink state.NewLinkText)

open Model
open View
open Update

/// Binds together the model, the view and the update.
let MyLinks (userDisplayName: string) =
    App.Create (InitialState userDisplayName) Update Page
    |> App.WithInitAction (Command (fun dispatch -> dispatch Refresh))
    |> App.Run
