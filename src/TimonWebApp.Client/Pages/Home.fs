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
open TimonWebApp.Client.Pages.Components.LinkViewList
open TimonWebApp.Client.Services

type Model =
    { addLinkBoxModel: AddLinkBox.Model
      linkViewListModel: LinkViewList.Model
      clubLinkViewListModel: ClubLinkViewList.Model
      searchBoxModel: SearchBox.Model
      homeSidebar: HomeSidebar.Model
      clubName: string
      clubId: Guid
      channelName: string
      channelId: Guid
      page: int
      tagName: string
      showNext: bool
      term: string
      activeMenuSection: MenuSection }
    static member Default =
        { linkViewListModel = LinkViewList.Model.Default
          clubLinkViewListModel = ClubLinkViewList.Model.Default
          addLinkBoxModel = AddLinkBox.Model.Default
          searchBoxModel = SearchBox.Model.Default
          homeSidebar = HomeSidebar.Model.Default
          channelName = "all"
          channelId = Guid.Empty
          page = 0
          showNext = false
          tagName = String.Empty
          term = String.Empty
          activeMenuSection = MenuSection.Channel
          clubName = String.Empty
          clubId = Guid.Empty }

type Message =
    | LinksLoaded of GetLinksResult
    | LoadLinks of int
    | LoadClubLinks of ClubLinkViewList.ClubLoadListParams
    | ClubLinksLoaded of ClubListView
    | LoadLinksByTag of string * int
    | LoadClubLinksByTag of string * int
    | LoadSearch of string * int
    | AddLinkBoxMsg of AddLinkBox.Message
    | LinkViewItemMsg of LinkViewList.Message
    | ClubLinkViewItemMsg of ClubLinkViewList.Message
    | RecentTagsUpdated of string list
    | RecentSearchUpdated of string list
    | SearchBoxMsg of SearchBox.Message
    | HomeSidebarMsg of HomeSidebar.Message

let init (_: IJSRuntime)  =
    Model.Default, Cmd.ofMsg (LoadLinks(0))


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
          tagForm = { tags = "" } })
    |> Seq.toArray

let update (jsRuntime: IJSRuntime)
           (timonService: TimonService)
           (localStorage: ILocalStorageService)
           (message: Message)
           (model: Model) =

    match message, model with
    | RecentTagsUpdated recentTags, _ ->
        let recentTagsMenuModel =
            { model.homeSidebar.recentTagsMenuModel with tags = recentTags }

        let homeSidebar =
            { model.homeSidebar with recentTagsMenuModel = recentTagsMenuModel }

        { model with homeSidebar = homeSidebar }, Cmd.none

    | RecentSearchUpdated terms, _ ->
        let recentSearchMenuModel =
            { model.homeSidebar.recentSearchMenuModel with terms = terms }

        let homeSidebar =
            { model.homeSidebar with recentSearchMenuModel = recentSearchMenuModel }

        { model with homeSidebar = homeSidebar }, Cmd.none

    | LinksLoaded data, _ ->
        let linkViewFormList = mapDataLinksToView data

        let linkViewListModel =
            { model.linkViewListModel with
                  links = linkViewFormList }

        jsRuntime.InvokeVoidAsync("scroll", 0, 0).AsTask()
        |> Async.AwaitTask
        |> ignore

        printfn "Home.LinksLoaded %O" (linkViewListModel)

        { model with
              linkViewListModel = linkViewListModel
              page = data.Page
              showNext = data.ShowNext }, Cmd.none

    | LoadLinks (page), _ ->
        let queryParams: GetLinkParams = { page = page }

        let linksCmd =
            Cmd.OfAsync.either getLinks (timonService, queryParams) LinksLoaded raise

        { model with
              page = page
              activeMenuSection = MenuSection.Channel }, linksCmd

    | LoadClubLinks (arg: ClubLinkViewList.ClubLoadListParams), _ ->

      let shouldLoadChannels, clubName, clubId, channelName, channelId, page = arg

      let msg = ClubLinkViewList.Message.LoadClubLinks arg

      let clubLinkViewListModel, clubLinkViewListCmd =
        ClubLinkViewList.update jsRuntime timonService msg model.clubLinkViewListModel

      let homeSideBarModel = { model.homeSidebar with clubId = clubId}

      let batchCmds =
          [ Cmd.map ClubLinkViewItemMsg clubLinkViewListCmd ]
          @ match shouldLoadChannels with
            | true -> [ Cmd.map HomeSidebarMsg (Cmd.ofMsg HomeSidebar.Message.LoadChannels) ]
            | false -> [ Cmd.none ]

      { model with
            page = page
            clubName = clubName
            clubId = clubId
            channelName = channelName
            channelId = channelId
            activeMenuSection = MenuSection.Channel
            clubLinkViewListModel = clubLinkViewListModel
            homeSidebar = homeSideBarModel }, Cmd.batch batchCmds

    | LoadLinksByTag (tag, page), _ ->
        let queryParams = { tagName = tag; page = page }

        let linksCmd =
            Cmd.OfAsync.either getLinksByTag (timonService, queryParams) LinksLoaded raise

        let recentTagsMenuModel =
            { model.homeSidebar.recentTagsMenuModel with activeTag = tag }

        let homeSidebar =
            { model.homeSidebar with recentTagsMenuModel = recentTagsMenuModel }

        { model with
              page = page
              tagName = tag
              activeMenuSection = MenuSection.Tag
              homeSidebar = homeSidebar },
        linksCmd

    | LoadClubLinksByTag (tag, page), _ ->

        let arg = ( model.clubId, tag, page )

        let msg = ClubLinkViewList.Message.LoadClubLinksByTag (arg)

        let clubLinkViewListModel, clubLinkViewListCmd =
            ClubLinkViewList.update jsRuntime timonService msg model.clubLinkViewListModel

        let batchCmds = [ Cmd.map ClubLinkViewItemMsg clubLinkViewListCmd ]

        { model with
              page = page
              tagName = tag
              activeMenuSection = MenuSection.Tag
              clubLinkViewListModel = clubLinkViewListModel }, Cmd.batch batchCmds

    | AddLinkBoxMsg (AddLinkBox.Message.NotifyLinkAdded), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ -> Cmd.ofMsg (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page))

        model, cmd

    | AddLinkBoxMsg msg, _ ->
        let m, cmd =
            AddLinkBox.update timonService msg model.addLinkBoxModel

        { model with addLinkBoxModel = m }, Cmd.map AddLinkBoxMsg cmd

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinksSearch (term)), _ ->
        let channelMenuModel =
            { model.homeSidebar.channelMenuModel with activeChannelId = Guid.Empty }

        let homeSidebar =
            { model.homeSidebar with channelMenuModel = channelMenuModel }

        { model with
            homeSidebar = homeSidebar
            activeMenuSection = MenuSection.Search },
        Cmd.ofMsg (LoadSearch(term, 0))

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinksByTag (tag)), _ ->
        let channelMenuModel =
            { model.homeSidebar.channelMenuModel with activeChannelId = Guid.Empty }

        let homeSidebar =
            { model.homeSidebar with channelMenuModel = channelMenuModel }

        { model with
              homeSidebar = homeSidebar
              activeMenuSection = MenuSection.Tag },
        Cmd.ofMsg (LoadLinksByTag(tag, 0))

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinks (channelId, channel)), _ ->
        let channelMenuModel =
            { model.homeSidebar.channelMenuModel with activeChannelId = channelId }

        let homeSidebar =
            { model.homeSidebar with channelMenuModel = channelMenuModel }

        { model with
            homeSidebar = homeSidebar
            activeMenuSection = MenuSection.Channel },
        Cmd.ofMsg (LoadClubLinks(false, model.clubName, model.clubId, channel, channelId, 0))

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.NotifyTagsUpdated), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ -> Cmd.ofMsg (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page))

        model, cmd

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.OnLoadClubLinks data), _ ->

        let msg = ClubLinkViewList.Message.OnLoadClubLinks data
        let clubLinkViewListModel, _ =
            ClubLinkViewList.update jsRuntime timonService msg model.clubLinkViewListModel

        let addLinkBoxModel =
            { model.addLinkBoxModel with
                  channelName = model.channelName
                  channelId = model.channelId
                  activeSection = model.activeMenuSection
                  tagName = model.tagName
                  clubId = model.clubId }

        let channelMenuModel =
            { model.homeSidebar.channelMenuModel with activeChannelId = model.channelId }

        let homeSidebar =
            { model.homeSidebar with channelMenuModel = channelMenuModel }

        let cmdUpdateRecentTags =
            Cmd.OfAsync.either updateTagsLocalStorage (localStorage, model) RecentTagsUpdated raise

        let cmdUpdateRecentSearch =
            Cmd.OfAsync.either updateSearchLocalStorage (localStorage, model) RecentSearchUpdated raise

        jsRuntime.InvokeVoidAsync("scroll", 0, 0).AsTask()
        |> Async.AwaitTask
        |> ignore

        { model with
            clubLinkViewListModel = clubLinkViewListModel
            addLinkBoxModel = addLinkBoxModel
            page = data.Page
            homeSidebar = homeSidebar
            showNext = data.ShowNext },
        Cmd.batch [ cmdUpdateRecentTags
                    cmdUpdateRecentSearch ]

    | ClubLinkViewItemMsg msg, _ ->
        let m, cmd =
            ClubLinkViewList.update jsRuntime timonService msg model.clubLinkViewListModel

        { model with clubLinkViewListModel = m }, Cmd.map ClubLinkViewItemMsg cmd


    | SearchBoxMsg (SearchBox.Message.LoadSearch (term, page)), _ ->
        let queryParams = { term = term; page = page }

        let linksCmd =
            Cmd.OfAsync.either searchLinks (timonService, queryParams) LinksLoaded raise

        let searchBoxModel =
            { model.searchBoxModel with
                  term = term }

        { model with
              page = page
              term = term
              activeMenuSection = MenuSection.Search
              searchBoxModel = searchBoxModel },
        linksCmd

    | SearchBoxMsg msg, _ ->
        let m, cmd =
            SearchBox.update timonService msg model.searchBoxModel

        { model with searchBoxModel = m }, Cmd.map SearchBoxMsg cmd

    | HomeSidebarMsg (HomeSidebar.Message.LoadLinks (shouldLoadChannels, channelName, channelId, page ) ), _ ->

        let msg = HomeSidebar.Message.LoadLinks (shouldLoadChannels, channelName, channelId, page)
        let homeSidebar, cmd = HomeSidebar.update jsRuntime timonService msg model.homeSidebar

        let loadClubLinksArgs = shouldLoadChannels, model.clubName, model.clubId, channelName, channelId, page
        let cmdBatchs = [ Cmd.map HomeSidebarMsg cmd; Cmd.ofMsg (LoadClubLinks loadClubLinksArgs)]

        { model with homeSidebar = homeSidebar}, Cmd.batch cmdBatchs

    | HomeSidebarMsg (HomeSidebar.Message.LoadLinksByTag (tag, page) ), _ ->

        let msg = HomeSidebar.Message.LoadLinksByTag (tag, page)
        let homeSidebar, cmd = HomeSidebar.update jsRuntime timonService msg model.homeSidebar

        let cmdBatchs = [ Cmd.map HomeSidebarMsg cmd; Cmd.ofMsg (LoadClubLinksByTag (tag, page))]

        { model with homeSidebar = homeSidebar}, Cmd.batch cmdBatchs

    | HomeSidebarMsg msg, _ ->
        let m, cmd = HomeSidebar.update jsRuntime timonService msg model.homeSidebar

        { model with homeSidebar = m}, Cmd.map HomeSidebarMsg cmd


type HomeTemplate = Template<"wwwroot/home.html">

let previousButton (model: Model) dispatch =
    match model.linkViewListModel.links with
    | [||] -> empty
    | _ ->
        let isDisabled, onClick =
            match model.page with
            | 0 -> (true, (fun _ -> ()))
            | _ ->
                (false,
                 (fun _ ->
                     match model.activeMenuSection with
                     | Tag -> dispatch (LoadLinksByTag(model.tagName, model.page - 1))
                     | Channel -> dispatch (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page - 1))
                     | Search -> dispatch (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page - 1))))

        a [ attr.``class`` Bulma.``pagination-previous``
            attr.disabled isDisabled
            on.click onClick ] [
            text "Previous"
        ]

let nextButton (model: Model) dispatch =
    match model.linkViewListModel.links with
    | [||] -> empty
    | _ ->
        let isDisabled, onClick =
            match model.showNext
                  && model.linkViewListModel.links.Length > 0 with
            | false -> (true, (fun _ -> ()))
            | true ->
                (false,
                 (fun _ ->
                     match model.activeMenuSection with
                     | Tag -> dispatch (LoadLinksByTag(model.tagName, model.page + 1))
                     | Channel -> dispatch (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page + 1))
                     | Search -> dispatch (LoadClubLinks(false, model.clubName, model.clubId, model.channelName, model.channelId, model.page + 1))))

        a [ attr.``class`` Bulma.``pagination-next``
            attr.disabled isDisabled
            on.click onClick ] [
            text "Next"
        ]

let view authState model dispatch =
    let items =
        match authState with
        | AuthState.Success ->
            ClubLinkViewList.view authState model.clubLinkViewListModel (ClubLinkViewItemMsg >> dispatch)
        | _ ->
            LinkViewList.view model.linkViewListModel (LinkViewItemMsg >> dispatch)

    let addLinkBoxHole =
        AddLinkBox.view authState (model.addLinkBoxModel) (AddLinkBoxMsg >> dispatch)

    let homeSidebarHole =
        HomeSidebar.view authState (model.homeSidebar) (HomeSidebarMsg >> dispatch)

    let previousButton = previousButton model dispatch
    let nextButton = nextButton model dispatch

    let title =
        match authState with
        | AuthState.Success ->
            match model.activeMenuSection with
            | Channel ->
                cond (String.IsNullOrEmpty(model.channelName))
                <| function
                    | true -> empty
                    | false ->
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
                                    text model.channelName
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
        | _ -> empty

    let searchBox = SearchBox.view authState model.searchBoxModel (SearchBoxMsg >> dispatch)

    let emptyLinksHole =
        match authState with
        | AuthState.Success -> empty
        | _ -> ComponentsTemplate.EmptyListBanner().Elt()

    HomeTemplate()
        .LinkListHole(items)
        .MenuSidebar(homeSidebarHole)
        .AddLinkBoxHole(addLinkBoxHole)
        .LinksTitleSection(title)
        .PreviousButton(previousButton)
        .NextButton(nextButton)
        .SearchBox(searchBox)
        .EmptyLinksHole(emptyLinksHole)
        .Elt()
