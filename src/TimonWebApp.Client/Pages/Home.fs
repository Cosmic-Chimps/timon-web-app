module TimonWebApp.Client.Pages.Home

open System
open Blazored.LocalStorage
open Elmish
open Bolero
open Bolero.Html
open FSharp.Json
open Microsoft.JSInterop
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Components
open TimonWebApp.Client.Pages.Components.ChannelMenuForm
open TimonWebApp.Client.Pages.Components.LinkViewList
open TimonWebApp.Client.Services

type Model =
    { addLinkBoxModel: AddLinkBox.Model
      channelMenuModel: ChannelMenu.Model
      channelMenuFormModel: ChannelMenuForm.Model
      linkViewListModel: LinkViewList.Model
      recentTagsMenuModel: RecentTagsMenu.Model
      recentSearchMenuModel: RecentSearchMenu.Model
      searchBoxModel: SearchBox.Model
      channel: string
      channelId: Guid
      page: int
      tagName: string
      showNext: bool
      term: string
      activeMenuSection: MenuSection }
    static member Default =
        { linkViewListModel = LinkViewList.Model.Default
          addLinkBoxModel = AddLinkBox.Model.Default
          channelMenuModel = ChannelMenu.Model.Default
          channelMenuFormModel = ChannelMenuForm.Model.Default
          recentTagsMenuModel = RecentTagsMenu.Model.Default
          recentSearchMenuModel = RecentSearchMenu.Model.Default
          searchBoxModel = SearchBox.Model.Default
          channel = "all"
          channelId = Guid.Empty
          page = 0
          showNext = false
          tagName = String.Empty
          term = String.Empty
          activeMenuSection = MenuSection.Channel }

type Message =
    | LinksLoaded of GetLinksResult
    | ChannelsLoaded of ChannelView array
    | LoadLinks of bool * string * Guid * int
    | LoadLinksByTag of string * int
    | LoadSearch of string * int
    | LoadChannels
    | AddLinkBoxMsg of AddLinkBox.Message
    | ChannelMenuMsg of ChannelMenu.Message
    | ChannelMenuFormMsg of ChannelMenuForm.Message
    | LinkViewItemMsg of LinkViewList.Message
    | RecentTagsMenuMsg of RecentTagsMenu.Message
    | RecentSearchMenuMsg of RecentSearchMenu.Message
    | RecentTagsUpdated of string list
    | RecentSearchUpdated of string list
    | SearchBoxMsg of SearchBox.Message

let init (_: IJSRuntime) (channel: string) =
    { Model.Default with channel = channel }, Cmd.ofMsg (LoadLinks(true, channel, Guid.Empty, 0))


let updateTagsLocalStorage ((localStorage: ILocalStorageService), (model: Model)) =
    async {
        let key = "timon_recent_tags"

        let! exists =
            localStorage.ContainKeyAsync(key).AsTask()
            |> Async.AwaitTask

        match exists with
        | false ->
            match model.tagName <> String.Empty with
            | false -> return []
            | true ->
                localStorage.SetItemAsync<string list>(key, [ model.tagName ]).AsTask()
                |> Async.AwaitTask
                |> ignore

                return [ model.tagName ]
        | true ->
            let! recentTagsJson =
                localStorage.GetItemAsStringAsync(key).AsTask()
                |> Async.AwaitTask

            let recentTags = Json.deserialize (recentTagsJson)

            match model.tagName <> String.Empty with
            | false -> return recentTags
            | true ->
                return recentTags
                       |> List.tryFind (fun tag -> tag = model.tagName)
                       |> fun found ->
                           match found with
                           | Some _ -> recentTags
                           | None _ ->
                               let recentTags' =
                                   match recentTags.Length + 1 > 9 with
                                   | false -> [ model.tagName ] @ recentTags
                                   | true ->
                                       [ model.tagName ]
                                       @ recentTags.[..recentTags.Length - 2]

                               localStorage.SetItemAsync<string list>(key, recentTags').AsTask()
                               |> Async.AwaitTask
                               |> ignore
                               recentTags'
    }

let updateSearchLocalStorage ((localStorage: ILocalStorageService), (model: Model)) =
    async {
        let key = "timon_recent_search"

        let! exists =
            localStorage.ContainKeyAsync(key).AsTask()
            |> Async.AwaitTask

        match exists with
        | false ->
            match model.term <> String.Empty with
            | false -> return []
            | true ->
                localStorage.SetItemAsync<string list>(key, [ model.term ]).AsTask()
                |> Async.AwaitTask
                |> ignore

                return [ model.term ]
        | true ->
            let! recentSearchJson =
                localStorage.GetItemAsStringAsync(key).AsTask()
                |> Async.AwaitTask

            let recentSearch = Json.deserialize (recentSearchJson)

            match model.term <> String.Empty with
            | false -> return recentSearch
            | true ->
                return recentSearch
                       |> List.tryFind (fun tag -> tag = model.term)
                       |> fun found ->
                           match found with
                           | Some _ -> recentSearch
                           | None _ ->
                               let recentSearch' =
                                   match recentSearch.Length + 1 > 9 with
                                   | false -> [ model.term ] @ recentSearch
                                   | true ->
                                       [ model.term ]
                                       @ recentSearch.[..recentSearch.Length - 2]

                               localStorage.SetItemAsync<string list>(key, recentSearch').AsTask()
                               |> Async.AwaitTask
                               |> ignore
                               recentSearch'
    }


let mapDataLinksToView (dataLinks: GetLinksResult) =
    dataLinks.Links
    |> Seq.map (fun lv ->
        { view = lv
          isTagFormOpen = false
          errorValidateForm = None
          tagForm = { tags = "" }
          openMoreInfo = false })
    |> Seq.toArray

let update (jsRuntime: IJSRuntime)
           (timonService: TimonService)
           (localStorage: ILocalStorageService)
           (message: Message)
           (model: Model)
           =
    match message, model with
    | RecentTagsUpdated recentTags, _ ->
        let recentTagsModel =
            { model.recentTagsMenuModel with
                  tags = recentTags }

        { model with
              recentTagsMenuModel = recentTagsModel },
        Cmd.none
    | RecentSearchUpdated terms, _ ->
        let recentSearchModel =
            { model.recentSearchMenuModel with
                  terms = terms }

        { model with
              recentSearchMenuModel = recentSearchModel },
        Cmd.none
    | LinksLoaded data, _ ->
        let linkViewFormList = mapDataLinksToView data

        let linkViewListModel =
            { model.linkViewListModel with
                  links = linkViewFormList }

        let addLinkBoxModel =
            { model.addLinkBoxModel with
                  channelName = model.channel
                  channelId = model.channelId
                  activeSection = model.activeMenuSection
                  tagName = model.tagName }

        let channelModel =
            { model.channelMenuModel with
                  activeChannelId = model.channelId }

        let cmdUpdateRecentTags =
            Cmd.ofAsync updateTagsLocalStorage (localStorage, model) RecentTagsUpdated raise

        let cmdUpdateRecentSearch =
            Cmd.ofAsync updateSearchLocalStorage (localStorage, model) RecentSearchUpdated raise

        jsRuntime.InvokeVoidAsync("scroll", 0, 0).AsTask()
        |> Async.AwaitTask
        |> ignore

        { model with
              linkViewListModel = linkViewListModel
              addLinkBoxModel = addLinkBoxModel
              page = data.Page
              channelMenuModel = channelModel
              showNext = data.ShowNext },
        Cmd.batch [ cmdUpdateRecentTags; cmdUpdateRecentSearch]
    | ChannelsLoaded channels, _ ->
        let channelMenuModel =
            { model.channelMenuModel with
                  channels = channels }

        { model with
              channelMenuModel = channelMenuModel },
        Cmd.none
    | LoadLinks (loadChannels, channel, channelId, page), _ ->
        let queryParams = { channelId = channelId; page = page }

        let linksCmd =
            Cmd.ofAsync getLinks (timonService, queryParams) LinksLoaded raise

        let batchCmds =
            [ linksCmd ]
            @ match loadChannels with
              | true -> [ Cmd.ofMsg LoadChannels ]
              | false -> [ Cmd.none ]

        { model with
              channel = channel
              channelId = channelId
              page = page
              activeMenuSection = MenuSection.Channel },
        Cmd.batch batchCmds

    | LoadLinksByTag (tag, page), _ ->
        let queryParams = { tagName = tag; page = page }

        let linksCmd =
            Cmd.ofAsync getLinksByTag (timonService, queryParams) LinksLoaded raise

        let recentTagsModel =
            { model.recentTagsMenuModel with
                  activeTag = tag }

        { model with
              page = page
              tagName = tag
              activeMenuSection = MenuSection.Tag
              recentTagsMenuModel = recentTagsModel },
        linksCmd

    | LoadSearch (term, page), _ ->
        let queryParams = { term = term; page = page }

        let linksCmd =
            Cmd.ofAsync searchLinks (timonService, queryParams) LinksLoaded raise

        let searchBoxModel =
            { model.searchBoxModel with
                  term = term }

        { model with
              page = page
              term = term
              activeMenuSection = MenuSection.Search
              searchBoxModel = searchBoxModel },
        linksCmd

    | LoadChannels, _ ->
        let cmd =
            Cmd.ofAsync getChannels timonService ChannelsLoaded raise

        model, cmd

    | AddLinkBoxMsg (AddLinkBox.Message.NotifyLinkAdded), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ -> Cmd.ofMsg (LoadLinks(false, model.channel, model.channelId, model.page))

        model, cmd
    | AddLinkBoxMsg msg, _ ->
        let m, cmd =
            AddLinkBox.update timonService msg model.addLinkBoxModel

        { model with addLinkBoxModel = m }, Cmd.map AddLinkBoxMsg cmd

    | ChannelMenuFormMsg (ChannelMenuForm.Message.NotifyChannelAdded), _ -> model, Cmd.ofMsg LoadChannels
    | ChannelMenuFormMsg msg, _ ->
        let m, cmd =
            ChannelMenuForm.update jsRuntime timonService msg model.channelMenuFormModel

        { model with channelMenuFormModel = m }, Cmd.map ChannelMenuFormMsg cmd

    | LinkViewItemMsg (LinkViewList.Message.LoadLinksByTag (tag)), _ ->
        let channelModel =
            { model.channelMenuModel with
                  activeChannelId = Guid.Empty }

        { model with
              channelMenuModel = channelModel
              activeMenuSection = MenuSection.Tag },
        Cmd.ofMsg (LoadLinksByTag(tag, 0))
    | LinkViewItemMsg (LinkViewList.Message.LoadLinks (channelId, channel)), _ ->
        let channelModel =
            { model.channelMenuModel with
                  activeChannelId = channelId }

        { model with
              channelMenuModel = channelModel
              activeMenuSection = MenuSection.Channel },
        Cmd.ofMsg (LoadLinks(false, channel, channelId, 0))
    | LinkViewItemMsg (LinkViewList.Message.NotifyTagsUpdated), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ -> Cmd.ofMsg (LoadLinks(false, model.channel, model.channelId, model.page))

        model, cmd
    | LinkViewItemMsg msg, _ ->
        let m, cmd =
            LinkViewList.update jsRuntime timonService msg model.linkViewListModel

        { model with linkViewListModel = m }, Cmd.map LinkViewItemMsg cmd

    | ChannelMenuMsg (ChannelMenu.Message.LoadLinks (channelId, channel)), _ ->
        let channelModel =
            { model.channelMenuModel with
                  activeChannelId = channelId
                  activeSection = Channel }

        { model with
              channelMenuModel = channelModel },
        Cmd.ofMsg (LoadLinks(false, channel, channelId, 0))
    | ChannelMenuMsg _, _ -> model, Cmd.none

    | RecentTagsMenuMsg (RecentTagsMenu.Message.LoadLinks (tag)), _ ->
        let channelModel =
            { model.channelMenuModel with
                  activeChannelId = Guid.Empty
                  activeSection = Tag }

        { model with
              channelMenuModel = channelModel },
        Cmd.ofMsg (LoadLinksByTag(tag, 0))

    | RecentSearchMenuMsg (RecentSearchMenu.Message.LoadLinks (term)), _ ->
        let recentSearchModel = { model.recentSearchMenuModel with activeTerm = term; activeSection = Search }
        { model with recentSearchMenuModel = recentSearchModel }, Cmd.ofMsg (LoadSearch(term, 0))

    | SearchBoxMsg (SearchBox.Message.Search (term)), _ ->
        { model with term = term }, Cmd.ofMsg (LoadSearch (term, 0))
    | SearchBoxMsg msg, _ ->
        let m, cmd =
            SearchBox.update timonService msg model.searchBoxModel

        { model with searchBoxModel = m }, Cmd.map LinkViewItemMsg cmd


type HomeTemplate = Template<"wwwroot/home.html">

let previousButton (model: Model) dispatch =
    let isDisabled, onClick =
        match model.page with
        | 0 -> (true, (fun _ -> ()))
        | _ ->
            (false,
             (fun _ ->
                 match model.activeMenuSection with
                 | Tag -> dispatch (LoadLinksByTag(model.tagName, model.page - 1))
                 | Channel -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page - 1))
                 | Search -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page - 1))))

    a [ attr.``class`` Bulma.``pagination-previous``
        attr.disabled isDisabled
        on.click onClick ] [
        text "Previous"
    ]

let nextButton (model: Model) dispatch =
    let isDisabled, onClick =
        match model.showNext
              && model.linkViewListModel.links.Length > 0 with
        | false -> (true, (fun _ -> ()))
        | true ->
            (false,
             (fun _ ->
                 match model.activeMenuSection with
                 | Tag -> dispatch (LoadLinksByTag(model.tagName, model.page + 1))
                 | Channel -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page + 1))
                 | Search -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page + 1))))

    a [ attr.``class`` Bulma.``pagination-next``
        attr.disabled isDisabled
        on.click onClick ] [
        text "Next"
    ]

let view authState model dispatch =
    let items =
        LinkViewList.view authState model.linkViewListModel (LinkViewItemMsg >> dispatch)

    let channelForm =
        ChannelMenuForm.view authState (model.channelMenuFormModel) (ChannelMenuFormMsg >> dispatch)

    let addLinkBoxHole =
        AddLinkBox.view authState (model.addLinkBoxModel) (AddLinkBoxMsg >> dispatch)

    let channels =
        ChannelMenu.view authState (model.channelMenuModel) model.activeMenuSection (ChannelMenuMsg >> dispatch)

    let menuTags =
        RecentTagsMenu.view (model.recentTagsMenuModel) model.activeMenuSection (RecentTagsMenuMsg >> dispatch)

    let recentSearchMenu =
        RecentSearchMenu.view (model.recentSearchMenuModel) model.activeMenuSection (RecentSearchMenuMsg >> dispatch)

    let previousButton = previousButton model dispatch
    let nextButton = nextButton model dispatch

    let isActiveClass =
        match model.channelId = Guid.Empty
              && model.activeMenuSection = Channel with
        | true -> Bulma.``is-active``
        | false -> ""

    let title =
        match model.activeMenuSection with
        | Channel ->
            h3 [ attr.``class``
                 <| String.concat
                     " "
                        [ Bulma.``is-3``
                          Bulma.``is-italic``
                          Bulma.title ] ] [
                text "channel: "
                span [ attr.``class``
                       <| String.concat
                           " "
                              [ Bulma.``has-text-weight-light``
                                Bulma.``is-italic`` ] ] [
                    cond (String.IsNullOrEmpty(model.channel))
                    <| function
                    | true -> text "all"
                    | false -> text model.channel
                ]
            ]
        | Tag ->
            h3 [ attr.``class``
                 <| String.concat
                     " "
                        [ Bulma.``is-3``
                          Bulma.``is-italic``
                          Bulma.title ] ] [
                text "tag: "
                span [ attr.``class``
                       <| String.concat
                           " "
                              [ Bulma.``has-text-weight-light``
                                Bulma.``is-italic`` ] ] [
                    text model.tagName
                ]
            ]
        | Search ->
            h3 [ attr.``class``
                 <| String.concat
                     " "
                        [ Bulma.``is-3``
                          Bulma.``is-italic``
                          Bulma.title ] ] [
                text "search: "
                span [ attr.``class``
                       <| String.concat
                           " "
                              [ Bulma.``has-text-weight-light``
                                Bulma.``is-italic`` ] ] [
                    text model.term
                ]
            ]

    let searchBox =
        SearchBox.view model.searchBoxModel (SearchBoxMsg >> dispatch)

    HomeTemplate()
        .LinkListHole(items)
        .ChannelForm(channelForm)
        .ChannelListHole(channels)
        .LoadAllLinks(fun _ -> (dispatch (LoadLinks(false, String.Empty, Guid.Empty, 0))))
        .AddLinkBoxHole(addLinkBoxHole)
        .LinksTitleSection(title)
        .PreviousButton(previousButton)
        .AllActiveClass(isActiveClass)
        .NextButton(nextButton)
        .SearchBox(searchBox)
        .RecentTagListHole(menuTags)
        .RecentSearchListHole(recentSearchMenu)
        .Elt()
