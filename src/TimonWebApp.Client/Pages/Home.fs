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
open TimonWebApp.Client.Pages.Components.AnonymousLinkViewList
open TimonWebApp.Client.Services
open TimonWebApp.Client.Pages.Controls
open TimonWebApp.Client.ChannelServices
open TimonWebApp.Client.Dtos

type Model =
    { addLinkBoxModel: AddLinkBox.Model
      anonymouslinkViewListModel: AnonymousLinkViewList.Model
      clubLinkViewListModel: ClubLinkViewList.Model
      searchBoxModel: SearchBox.Model
      clubSidebarModel: ClubSidebar.Model
      homeSidebarModel: HomeSidebar.Model
      clubName: string
      clubId: Guid
      channelName: string
      channelId: Guid
      channel: ChannelView option
      page: int
      tagName: string
      showNext: bool
      term: string
      activeMenuSection: MenuSection
      shouldReloadHomeSidebar: bool
      showChannelModal: bool
      channelSettingsTabControlModel: ChannelSettingsTabControl.Model }
    static member Default =
        { anonymouslinkViewListModel = AnonymousLinkViewList.Model.Default
          clubLinkViewListModel = ClubLinkViewList.Model.Default
          addLinkBoxModel = AddLinkBox.Model.Default
          searchBoxModel = SearchBox.Model.Default
          homeSidebarModel = HomeSidebar.Model.Default
          clubSidebarModel = ClubSidebar.Model.Default
          channelName = "all"
          channelId = Guid.Empty
          page = 0
          showNext = false
          tagName = String.Empty
          term = String.Empty
          activeMenuSection = MenuSection.Channel
          clubName = String.Empty
          clubId = Guid.Empty
          shouldReloadHomeSidebar = true
          channelSettingsTabControlModel =
              ChannelSettingsTabControl.Model.Default
          showChannelModal = false
          channel = None }

type Message =
    // | LinksLoaded of GetLinksResult
    // | LoadLinks of int
    | LoadClubLinks of ClubLinkViewList.ClubLoadListParams
    | LoadLinksByTag of string * int
    | LoadClubLinksByTag of string * int
    | LoadClubLinksSearch of string * int
    | AddLinkBoxMsg of AddLinkBox.Message
    | AnonymousLinkViewItemMsg of AnonymousLinkViewList.Message
    | ClubLinkViewItemMsg of ClubLinkViewList.Message
    | SearchBoxMsg of SearchBox.Message
    | HomeSidebarMsg of HomeSidebar.Message
    | ClubSidebarMsg of ClubSidebar.Message
    | AnonymousLinkViewListMsg of AnonymousLinkViewList.Message
    | OpenChannelSettings
    | DismissSettingsModal
    | ChannelSettingsTabControlMsg of ChannelSettingsTabControl.Message

let init (_: IJSRuntime) (state: AuthState) =
    let cmdExecute =
        match state with
        | AuthState.Success ->
            let cmd = Cmd.ofMsg (ClubSidebar.LoadClubs)
            Cmd.map ClubSidebarMsg cmd
        | _ ->
            let cmd =
                Cmd.ofMsg (AnonymousLinkViewList.LoadLinks(0))

            Cmd.map AnonymousLinkViewListMsg cmd

    Model.Default, cmdExecute



let update
    (jsRuntime: IJSRuntime)
    (timonService: TimonService)
    (localStorage: ILocalStorageService)
    (message: Message)
    (model: Model)
    =

    match message, model with
    // | LoadLinks page, _ ->
    //     let
    | LoadClubLinks (arg: ClubLinkViewList.ClubLoadListParams), _ ->

        let shouldLoadChannels, clubName, clubId, channelName, channelId, page =
            arg

        let msg =
            ClubLinkViewList.Message.LoadClubLinks arg

        let clubLinkViewListModel, clubLinkViewListCmd =
            ClubLinkViewList.update
                jsRuntime
                timonService
                msg
                model.clubLinkViewListModel

        let homeSideBarModel =
            { model.homeSidebarModel with
                  clubId = clubId
                  channelId = channelId }

        let homeSideBarMsg =
            Cmd.ofMsg (HomeSidebar.Message.UpdateChannelId channelId)

        let searchBoxMessage =
            SearchBox.Message.UpdateInputSearchBox String.Empty

        let searchBoxModel, _ =
            SearchBox.update timonService searchBoxMessage model.searchBoxModel

        let clubSidebarMessage =
            ClubSidebar.Message.SetActiveClubId clubId

        let clubSidebarModel, _ =
            ClubSidebar.update
                jsRuntime
                timonService
                clubSidebarMessage
                model.clubSidebarModel

        let batchCmds =
            [ Cmd.map ClubLinkViewItemMsg clubLinkViewListCmd
              Cmd.map HomeSidebarMsg homeSideBarMsg ]
            @ match shouldLoadChannels with
              | true ->
                  [ Cmd.map
                        HomeSidebarMsg
                        (Cmd.ofMsg HomeSidebar.Message.LoadChannels) ]
              | false -> [ Cmd.none ]

        { model with
              page = page
              clubName = clubName
              clubId = clubId
              channelName = channelName
              channelId = channelId
              searchBoxModel = searchBoxModel
              activeMenuSection = MenuSection.Channel
              clubLinkViewListModel = clubLinkViewListModel
              homeSidebarModel = homeSideBarModel
              clubSidebarModel = clubSidebarModel },
        Cmd.batch batchCmds

    // | LoadLinksByTag (tag, page), _ ->
    //     let queryParams = { tagName = tag; page = page }

    //     let linksCmd =
    //         Cmd.OfAsync.either
    //             getLinksByTag
    //             (timonService, queryParams)
    //             LinksLoaded
    //             raise

    //     { model with
    //           page = page
    //           tagName = tag
    //           activeMenuSection = MenuSection.Tag },
    //     linksCmd

    | LoadClubLinksByTag (tag, page), _ ->
        let arg = (model.clubId, tag, page)

        let msg =
            ClubLinkViewList.Message.LoadClubLinksByTag(arg)

        let clubLinkViewListModel, clubLinkViewListCmd =
            ClubLinkViewList.update
                jsRuntime
                timonService
                msg
                model.clubLinkViewListModel

        let searchBoxMessage =
            SearchBox.Message.UpdateInputSearchBox String.Empty

        let searchBoxModel, _ =
            SearchBox.update timonService searchBoxMessage model.searchBoxModel

        let homeSidebarMessage =
            HomeSidebar.Message.LoadLinksByTag(tag, page)

        let homeSidebar, _ =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                homeSidebarMessage
                model.homeSidebarModel

        let batchCmds =
            [ Cmd.map ClubLinkViewItemMsg clubLinkViewListCmd ]

        { model with
              page = page
              tagName = tag
              activeMenuSection = MenuSection.Tag
              searchBoxModel = searchBoxModel
              homeSidebarModel = homeSidebar
              clubLinkViewListModel = clubLinkViewListModel },
        Cmd.batch batchCmds

    | LoadClubLinksSearch (term, page), _ ->
        let arg = (model.clubId, term, page)

        let msg =
            ClubLinkViewList.Message.LoadClubLinksBySearch(arg)

        let clubLinkViewListModel, clubLinkViewListCmd =
            ClubLinkViewList.update
                jsRuntime
                timonService
                msg
                model.clubLinkViewListModel

        let searchBoxMessage =
            SearchBox.Message.UpdateInputSearchBox term

        let searchBoxModel, _ =
            SearchBox.update timonService searchBoxMessage model.searchBoxModel

        let homeSidebarMessage =
            HomeSidebar.Message.LoadLinksBySearch(term, page)

        let homeSidebar, _ =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                homeSidebarMessage
                model.homeSidebarModel

        let batchCmds =
            [ Cmd.map ClubLinkViewItemMsg clubLinkViewListCmd ]

        { model with
              page = page
              term = term
              activeMenuSection = MenuSection.Search
              clubLinkViewListModel = clubLinkViewListModel
              homeSidebarModel = homeSidebar
              searchBoxModel = searchBoxModel },
        Cmd.batch batchCmds

    | AddLinkBoxMsg (AddLinkBox.Message.NotifyLinkAdded), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ ->
                Cmd.ofMsg (
                    LoadClubLinks(
                        false,
                        model.clubName,
                        model.clubId,
                        model.channelName,
                        model.channelId,
                        model.page
                    )
                )

        model, cmd

    | AddLinkBoxMsg msg, _ ->
        let m, cmd =
            AddLinkBox.update timonService msg model.addLinkBoxModel

        { model with addLinkBoxModel = m }, Cmd.map AddLinkBoxMsg cmd

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinksSearch (term)), _ ->
        { model with
              activeMenuSection = MenuSection.Search },
        Cmd.ofMsg (LoadClubLinksSearch(term, 0))

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinksByTag (tag)), _ ->
        let channelMenuModel =
            { model.homeSidebarModel.channelMenuModel with
                  activeChannelId = Guid.Empty }

        let homeSidebar =
            { model.homeSidebarModel with
                  channelMenuModel = channelMenuModel }

        { model with
              homeSidebarModel = homeSidebar
              activeMenuSection = MenuSection.Tag },
        Cmd.ofMsg (LoadClubLinksByTag(tag, 0))

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.LoadLinks (channelId,
                                                               channel)),
      _ ->
        let channelMenuModel =
            { model.homeSidebarModel.channelMenuModel with
                  activeChannelId = channelId }

        let homeSidebar =
            { model.homeSidebarModel with
                  channelMenuModel = channelMenuModel }

        { model with
              homeSidebarModel = homeSidebar
              activeMenuSection = MenuSection.Channel },
        Cmd.ofMsg (
            LoadClubLinks(
                false,
                model.clubName,
                model.clubId,
                channel,
                channelId,
                0
            )
        )

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.NotifyTagsUpdated), _ ->
        let cmd =
            match model.activeMenuSection with
            | Tag -> Cmd.ofMsg (LoadLinksByTag(model.tagName, model.page))
            | _ ->
                Cmd.ofMsg (
                    LoadClubLinks(
                        false,
                        model.clubName,
                        model.clubId,
                        model.channelName,
                        model.channelId,
                        model.page
                    )
                )

        model, cmd

    | ClubLinkViewItemMsg (ClubLinkViewList.Message.ClubLinksLoaded data), _ ->

        let msg =
            ClubLinkViewList.Message.ClubLinksLoaded data

        let clubLinkViewListModel, _ =
            ClubLinkViewList.update
                jsRuntime
                timonService
                msg
                model.clubLinkViewListModel

        let addLinkBoxModel =
            { model.addLinkBoxModel with
                  channelName = model.channelName
                  channelId = model.channelId
                  activeSection = model.activeMenuSection
                  tagName = model.tagName
                  clubId = model.clubId }

        let loadTagsMessage =
            HomeSidebar.Message.RecentTagsMenuMsg(
                RecentTagsMenu.Message.LoadTags(model.clubId)
            )

        let homeSidebarModel, cmdUpdateRecentTags =
            match model.shouldReloadHomeSidebar with
            | false -> model.homeSidebarModel, Cmd.none
            | true ->
                HomeSidebar.update
                    jsRuntime
                    timonService
                    localStorage
                    loadTagsMessage
                    model.homeSidebarModel

        let model' =
            { model with
                  homeSidebarModel = homeSidebarModel }


        let loadTermsMessage =
            HomeSidebar.Message.RecentSearchMenuMsg(
                RecentSearchMenu.Message.LoadTerms(model.clubId)
            )

        let homeSidebarModel', cmdUpdateRecentTerms =
            match model.shouldReloadHomeSidebar with
            | false -> model.homeSidebarModel, Cmd.none
            | true ->
                HomeSidebar.update
                    jsRuntime
                    timonService
                    localStorage
                    loadTermsMessage
                    model'.homeSidebarModel

        jsRuntime
            .InvokeVoidAsync("scroll", 0, 0)
            .AsTask()
        |> Async.AwaitTask
        |> ignore

        { model with
              clubLinkViewListModel = clubLinkViewListModel
              addLinkBoxModel = addLinkBoxModel
              homeSidebarModel = homeSidebarModel'
              page = data.Page
              shouldReloadHomeSidebar = false
              showNext = data.ShowNext },
        Cmd.batch [
            Cmd.map HomeSidebarMsg cmdUpdateRecentTags
            Cmd.map HomeSidebarMsg cmdUpdateRecentTerms
        ]

    | ClubLinkViewItemMsg msg, _ ->
        let m, cmd =
            ClubLinkViewList.update
                jsRuntime
                timonService
                msg
                model.clubLinkViewListModel

        { model with clubLinkViewListModel = m },
        Cmd.map ClubLinkViewItemMsg cmd


    | SearchBoxMsg (SearchBox.Message.LoadSearch (term, page)), _ ->
        let cmdBatchs =
            [ Cmd.ofMsg (LoadClubLinksSearch(term, page)) ]

        let searchBoxModel =
            { model.searchBoxModel with
                  term = term }

        { model with
              page = page
              term = term
              activeMenuSection = MenuSection.Search
              searchBoxModel = searchBoxModel },
        Cmd.batch cmdBatchs
    | SearchBoxMsg (SearchBox.Message.ToggleSidebarVisibility), _ ->
        let clubSidebarMessage =
            ClubSidebar.Message.ToggleSidebarVisibility

        model, Cmd.ofMsg (ClubSidebarMsg clubSidebarMessage)

    | SearchBoxMsg msg, _ ->
        let m, cmd =
            SearchBox.update timonService msg model.searchBoxModel

        { model with searchBoxModel = m }, Cmd.map SearchBoxMsg cmd

    | HomeSidebarMsg (HomeSidebar.Message.LoadLinks (shouldLoadChannels,
                                                     channelName,
                                                     channelId,
                                                     page)),
      _ ->

        let msg =
            HomeSidebar.Message.LoadLinks(
                shouldLoadChannels,
                channelName,
                channelId,
                page
            )

        let homeSidebar, cmd =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                msg
                model.homeSidebarModel

        let loadClubLinksArgs =
            shouldLoadChannels,
            model.clubName,
            model.clubId,
            channelName,
            channelId,
            page

        let cmdBatchs =
            [ Cmd.map HomeSidebarMsg cmd
              Cmd.ofMsg (LoadClubLinks loadClubLinksArgs) ]

        { model with
              homeSidebarModel = homeSidebar },
        Cmd.batch cmdBatchs

    | HomeSidebarMsg (HomeSidebar.Message.LoadLinksByTag (tag, page)), _ ->

        let msg =
            HomeSidebar.Message.LoadLinksByTag(tag, page)

        let homeSidebar, cmd =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                msg
                model.homeSidebarModel

        let cmdBatchs =
            [ Cmd.map HomeSidebarMsg cmd
              Cmd.ofMsg (LoadClubLinksByTag(tag, page)) ]

        { model with
              homeSidebarModel = homeSidebar },
        Cmd.batch cmdBatchs

    | HomeSidebarMsg (HomeSidebar.Message.LoadLinksBySearch (term, page)), _ ->
        let msg =
            HomeSidebar.Message.LoadLinksBySearch(term, page)

        let homeSidebar, cmd =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                msg
                model.homeSidebarModel

        let cmdBatchs =
            [ Cmd.map HomeSidebarMsg cmd
              Cmd.ofMsg (LoadClubLinksSearch(term, page)) ]

        { model with
              homeSidebarModel = homeSidebar },
        Cmd.batch cmdBatchs

    | HomeSidebarMsg msg, _ ->
        let m, cmd =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                msg
                model.homeSidebarModel

        { model with homeSidebarModel = m }, Cmd.map HomeSidebarMsg cmd

    | ClubSidebarMsg (ClubSidebar.Message.ChangeClub (clubView)), _ ->
        let cmd =
            Cmd.ofMsg (
                LoadClubLinks(
                    true,
                    clubView.Name,
                    clubView.Id,
                    String.Empty,
                    Guid.Empty,
                    0
                )
            )

        let clubMsg = ClubSidebar.Message.ChangeClub(clubView)

        let clubSidebarModel, _ =
            ClubSidebar.update
                jsRuntime
                timonService
                clubMsg
                model.clubSidebarModel

        let homeSidebarMsg = HomeSidebar.Message.ResetComponents

        let homeSidebarModel, _ =
            HomeSidebar.update
                jsRuntime
                timonService
                localStorage
                homeSidebarMsg
                model.homeSidebarModel

        let searchBoxModel =
            { model.searchBoxModel with
                  clubName = clubView.Name }

        { model with
              searchBoxModel = searchBoxModel
              clubSidebarModel = clubSidebarModel
              homeSidebarModel = homeSidebarModel
              shouldReloadHomeSidebar = true },
        cmd

    | ClubSidebarMsg (ClubSidebar.Message.NoClubs), _ ->
        let cmd =
            Cmd.ofMsg (AnonymousLinkViewList.LoadLinks(0))

        model, Cmd.map AnonymousLinkViewListMsg cmd

    | ClubSidebarMsg msg, _ ->
        let clubSidebarModel, cmd =
            ClubSidebar.update jsRuntime timonService msg model.clubSidebarModel

        { model with
              clubSidebarModel = clubSidebarModel },
        Cmd.map ClubSidebarMsg cmd

    | AnonymousLinkViewListMsg (AnonymousLinkViewList.Message.LinksLoaded data),
      _ ->
        let anonymousMsg =
            AnonymousLinkViewList.Message.LinksLoaded data

        let anonymouslinkViewListModel, cmd =
            AnonymousLinkViewList.update
                jsRuntime
                timonService
                anonymousMsg
                model.anonymouslinkViewListModel

        { model with
              anonymouslinkViewListModel = anonymouslinkViewListModel
              page = data.Page
              showNext = data.ShowNext },
        Cmd.map AnonymousLinkViewListMsg cmd

    | AnonymousLinkViewListMsg (AnonymousLinkViewList.Message.LoadLinks page), _ ->
        let anonymousMsg =
            AnonymousLinkViewList.Message.LoadLinks page

        let anonymouslinkViewListModel, cmd =
            AnonymousLinkViewList.update
                jsRuntime
                timonService
                anonymousMsg
                model.anonymouslinkViewListModel

        { model with
              anonymouslinkViewListModel = anonymouslinkViewListModel
              page = page
              activeMenuSection = MenuSection.Channel },
        Cmd.map AnonymousLinkViewListMsg cmd

    | OpenChannelSettings _, _ ->
        let channelSettingsTabControlMsg =
            ChannelSettingsTabControl.Message.SetChannel(
                model.clubId,
                model.channelId,
                model.channelName
            )

        let channelSettingsTabControlModel, cmd =
            ChannelSettingsTabControl.update
                timonService
                model.channelSettingsTabControlModel
                channelSettingsTabControlMsg

        { model with
              channelSettingsTabControlModel = channelSettingsTabControlModel
              showChannelModal = true },
        Cmd.map ChannelSettingsTabControlMsg cmd

    | DismissSettingsModal _, _ ->
        let channelSettingsTabControlMsg =
            ChannelSettingsTabControl.Message.ResetModel

        let channelSettingsTabControlModel, _ =
            ChannelSettingsTabControl.update
                timonService
                model.channelSettingsTabControlModel
                channelSettingsTabControlMsg

        { model with
              channelSettingsTabControlModel = channelSettingsTabControlModel
              showChannelModal = false },
        Cmd.none

    | ChannelSettingsTabControlMsg msg, _ ->
        let channelSettingsTabModal, cmd =
            ChannelSettingsTabControl.update
                timonService
                model.channelSettingsTabControlModel
                msg

        { model with
              channelSettingsTabControlModel = channelSettingsTabModal },
        Cmd.map ChannelSettingsTabControlMsg cmd

// | AnonymousLinkViewListMsg msg, _ ->
//     let anonymouslinkViewListModel, cmd =
//         AnonymousLinkViewList.update
//             jsRuntime
//             timonService
//             msg
//             model.anonymouslinkViewListModel

//     { model with
//           anonymouslinkViewListModel = anonymouslinkViewListModel },
//     Cmd.map AnonymousLinkViewListMsg cmd



type HomeTemplate = Template<"wwwroot/home.html">

let previousButton (model: Model) dispatch =
    match model.anonymouslinkViewListModel.links with
    | [||] -> empty
    | _ ->
        let isDisabled, onClick =
            match model.page with
            | 0 -> (true, (fun _ -> ()))
            | _ ->
                (false,
                 (fun _ ->
                     match model.activeMenuSection with
                     | Tag ->
                         dispatch (
                             LoadLinksByTag(model.tagName, model.page - 1)
                         )
                     | Channel ->
                         dispatch (
                             LoadClubLinks(
                                 false,
                                 model.clubName,
                                 model.clubId,
                                 model.channelName,
                                 model.channelId,
                                 model.page - 1
                             )
                         )
                     | Search ->
                         dispatch (
                             LoadClubLinks(
                                 false,
                                 model.clubName,
                                 model.clubId,
                                 model.channelName,
                                 model.channelId,
                                 model.page - 1
                             )
                         )))

        a [ attr.``class`` Bulma.``pagination-previous``
            attr.disabled isDisabled
            on.click onClick ] [
            text "Previous"
        ]

let nextButton (model: Model) dispatch =
    match model.anonymouslinkViewListModel.links with
    | [||] -> empty
    | _ ->
        let isDisabled, onClick =
            match model.showNext
                  && model.anonymouslinkViewListModel.links.Length > 0 with
            | false -> (true, (fun _ -> ()))
            | true ->
                (false,
                 (fun _ ->
                     match model.activeMenuSection with
                     | Tag ->
                         dispatch (
                             LoadLinksByTag(model.tagName, model.page + 1)
                         )
                     | Channel ->
                         dispatch (
                             LoadClubLinks(
                                 false,
                                 model.clubName,
                                 model.clubId,
                                 model.channelName,
                                 model.channelId,
                                 model.page + 1
                             )
                         )
                     | Search ->
                         dispatch (
                             LoadClubLinks(
                                 false,
                                 model.clubName,
                                 model.clubId,
                                 model.channelName,
                                 model.channelId,
                                 model.page + 1
                             )
                         )))

        a [ attr.``class`` Bulma.``pagination-next``
            attr.disabled isDisabled
            on.click onClick ] [
            text "Next"
        ]

let anonymousListView model dispatch =
    AnonymousLinkViewList.view
        model.anonymouslinkViewListModel
        (AnonymousLinkViewItemMsg
         >> dispatch)

let areAllComponentsReady model =
    model.clubLinkViewListModel.isReady
    && HomeSidebar.isReady model.homeSidebarModel
// && model.homeSidebar.channelMenuModel.isReady
// && model.homeSidebar.recentSearchMenuModel.isReady
// && model.homeSidebar.recentSearchMenuModel.isReady

let view authState model dispatch =
    let hasClubs =
        model.clubSidebarModel.clubs.Length
        <> 0

    if not (areAllComponentsReady model)
       && authState = AuthState.Success
       && hasClubs then
        printfn "one"

        ComponentsTemplate
            .LoadingTemplate()
            .Elt()
    else if not (model.anonymouslinkViewListModel.isReady)
            && authState
               <> AuthState.Success then
        printfn "two"

        ComponentsTemplate
            .LoadingTemplate()
            .Elt()
    else if not (model.anonymouslinkViewListModel.isReady)
            && authState = AuthState.Success
            && not hasClubs then
        ComponentsTemplate
            .LoadingTemplate()
            .Elt()
    else
        let items =
            match authState with
            | AuthState.Success ->
                match hasClubs with
                | true ->
                    ClubLinkViewList.view
                        authState
                        model.clubLinkViewListModel
                        (ClubLinkViewItemMsg
                         >> dispatch)
                | false -> anonymousListView model dispatch

            | _ -> anonymousListView model dispatch


        let addLinkBoxHole =
            match hasClubs with
            | true ->
                AddLinkBox.view
                    authState
                    (model.addLinkBoxModel)
                    (AddLinkBoxMsg
                     >> dispatch)
            | false -> empty

        let homeSidebarHole =
            match hasClubs with
            | true ->
                HomeSidebar.view
                    authState
                    (model.homeSidebarModel)
                    (HomeSidebarMsg
                     >> dispatch)
            | false -> empty

        let previousButton = previousButton model dispatch
        let nextButton = nextButton model dispatch

        let title =
            match authState with
            | AuthState.Success ->
                match hasClubs with
                | false -> empty
                | true ->
                    match model.activeMenuSection with
                    | Channel ->
                        cond (String.IsNullOrEmpty(model.channelName))
                        <| function
                        | true -> empty
                        | false ->
                            div [ attr.``class``
                                  <| Bulma.columns ] [
                                div [ attr.``class``
                                      <| Bulma.column ] [
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
                                ]
                                div [ attr.``class``
                                      <| Bulma.column ] [
                                    a [ attr.``class``
                                        <| Bulma.``is-pulled-right``
                                        on.click
                                            (fun _ ->
                                                dispatch OpenChannelSettings) ] [
                                        i [ attr.``class``
                                            <| String.concat
                                                " "
                                                [ Mdi.mdi; Mdi.``mdi-cog`` ] ] []
                                    ]
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

        let searchBox =
            match hasClubs with
            | false -> empty
            | true ->
                SearchBox.view
                    authState
                    model.searchBoxModel
                    (SearchBoxMsg
                     >> dispatch)

        let emptyLinksHole =
            match authState with
            | AuthState.Success ->
                match hasClubs with
                | true -> empty
                | false ->
                    let message =
                        ClubSidebarMsg(
                            ClubSidebar.Message.ToggleSidebarVisibility
                        )

                    // Model.Default, Cmd.map AnonymousLinkViewListMsg cmd

                    ComponentsTemplate
                        .SubscribeToClubBanner()
                        .ShowClubSidebar(fun _ -> dispatch message)
                        .Elt()

            | _ ->
                ComponentsTemplate
                    .EmptyListBanner()
                    .Elt()

        let clubSidebar =
            match authState with
            | AuthState.Success ->
                ClubSidebar.view
                    model.clubSidebarModel
                    (ClubSidebarMsg
                     >> dispatch)
            | _ -> empty

        let channelSettingsModal =
            match model.showChannelModal with
            | false -> empty
            | true ->
                let tabControl =
                    ChannelSettingsTabControl.view
                        model.channelSettingsTabControlModel
                        (ChannelSettingsTabControlMsg
                         >> dispatch)

                ComponentsTemplate
                    .ChannelSettingsModal()
                    .ChannelName(model.channelName)
                    .DismissModal(fun _ -> dispatch DismissSettingsModal)
                    .ChannelSettingsTabControl(tabControl)
                    .Elt()

        HomeTemplate()
            .ClubSidebar(clubSidebar)
            .LinkListHole(items)
            .MenuSidebar(homeSidebarHole)
            .AddLinkBoxHole(addLinkBoxHole)
            .LinksTitleSection(title)
            .PreviousButton(previousButton)
            .NextButton(nextButton)
            .SearchBox(searchBox)
            .ChannelSettingsModal(channelSettingsModal)
            .EmptyLinksHole(emptyLinksHole)
            .Elt()
