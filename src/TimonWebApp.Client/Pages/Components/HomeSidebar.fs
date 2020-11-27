module TimonWebApp.Client.Pages.Components.HomeSidebar

open System
open System.ComponentModel.Design
open System.Net
open Bolero
open Elmish
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Controls
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open Bolero.Html
open Microsoft.JSInterop
open Blazored.LocalStorage


type Model =
    { channelMenuModel: ChannelMenu.Model
      channelMenuFormModel: ChannelMenuForm.Model
      recentTagsMenuModel: RecentTagsMenu.Model
      recentSearchMenuModel: RecentSearchMenu.Model
      activeMenuSection: MenuSection
      channelId: ChannelId
      tag: string
      term: string
      clubId: ClubId
      authState: AuthState }
    static member Default =
        { channelMenuModel = ChannelMenu.Model.Default
          channelMenuFormModel = ChannelMenuForm.Model.Default
          recentTagsMenuModel = RecentTagsMenu.Model.Default
          recentSearchMenuModel = RecentSearchMenu.Model.Default
          activeMenuSection = MenuSection.Channel
          channelId = Guid.Empty
          authState = AuthState.NotTried
          clubId = Guid.Empty
          tag = String.Empty
          term = String.Empty }

type Message =
    | ChannelMenuMsg of ChannelMenu.Message
    | ChannelMenuFormMsg of ChannelMenuForm.Message
    | RecentTagsMenuMsg of RecentTagsMenu.Message
    | RecentSearchMenuMsg of RecentSearchMenu.Message
    | LoadChannels
    | ChannelsLoaded of ChannelView array
    | LoadLinks of bool * string * ChannelId * int
    | LoadLinksByTag of string * int
    | LoadLinksBySearch of string * int
    | UpdateChannelId of ChannelId
    | ResetComponents

let isReady model =
    printfn "bbbbb %O" model.channelMenuModel.isReady
    printfn "ccccc %O" model.recentTagsMenuModel.isReady
    printfn "ddddd %O" model.recentSearchMenuModel.isReady

    model.channelMenuModel.isReady
    && model.recentTagsMenuModel.isReady
    && model.recentSearchMenuModel.isReady

let update (jsRuntime: IJSRuntime)
           (timonService: TimonService)
           (localStorage: ILocalStorageService)
           (message: Message)
           (model: Model)
           =

    match message, model with
    | LoadChannels, _ ->
        let cmd =
            Cmd.OfAsync.either
                getChannels
                (timonService, model.clubId)
                ChannelsLoaded
                raise

        let channelMenuFormModel =
            { model.channelMenuFormModel with
                  clubId = model.clubId }

        { model with
              channelMenuFormModel = channelMenuFormModel },
        cmd

    | ChannelsLoaded channels, _ ->
        let msg =
            ChannelMenu.Message.OnChannelsLoaded channels

        let channelMenuModel, cmd =
            ChannelMenu.update msg model.channelMenuModel

        let cmds = [ cmd; Cmd.none ]

        { model with
              channelMenuModel = channelMenuModel },
        Cmd.batch cmds

    | ChannelMenuFormMsg (ChannelMenuForm.Message.NotifyChannelAdded), _ ->
        model, Cmd.ofMsg LoadChannels

    | ChannelMenuFormMsg msg, _ ->
        let m, cmd =
            ChannelMenuForm.update
                jsRuntime
                timonService
                msg
                model.channelMenuFormModel

        { model with channelMenuFormModel = m }, Cmd.map ChannelMenuFormMsg cmd

    | ChannelMenuMsg (ChannelMenu.Message.LoadLinks (channelId,
                                                     channel,
                                                     activeSection)),
      _ ->
        let msg =
            ChannelMenu.Message.LoadLinks(channelId, channel, activeSection)

        let channelMenuModel, cmd =
            ChannelMenu.update msg model.channelMenuModel

        let recentTagsMenuModel =
            { model.recentTagsMenuModel with
                  activeSection = activeSection }

        let recentSearchMenuModel =
            { model.recentSearchMenuModel with
                  activeSection = activeSection }

        let cmdBatch =
            [ cmd
              Cmd.ofMsg (LoadLinks(false, channel, channelId, 0)) ]

        { model with
              channelMenuModel = channelMenuModel
              recentTagsMenuModel = recentTagsMenuModel
              recentSearchMenuModel = recentSearchMenuModel
              activeMenuSection = activeSection },
        Cmd.batch cmdBatch

    | ChannelMenuMsg _, _ -> model, Cmd.none

    | RecentTagsMenuMsg (RecentTagsMenu.Message.LoadLinks (tag, activeSection)),
      _ ->
        let msg =
            RecentTagsMenu.Message.LoadLinks(tag, activeSection)

        let recentTagsMenuModel, cmd =
            RecentTagsMenu.update localStorage msg model.recentTagsMenuModel

        let channelMenuModel =
            { model.channelMenuModel with
                  activeSection = activeSection }

        let recentSearchMenuModel =
            { model.recentSearchMenuModel with
                  activeSection = activeSection }

        let cmdBatch =
            [ Cmd.map RecentTagsMenuMsg cmd
              Cmd.ofMsg (LoadLinksByTag(tag, 0)) ]

        { model with
              recentTagsMenuModel = recentTagsMenuModel
              channelMenuModel = channelMenuModel
              recentSearchMenuModel = recentSearchMenuModel
              activeMenuSection = activeSection },
        Cmd.batch cmdBatch

    | RecentTagsMenuMsg msg, _ ->

        let recentTagsMenuModel, cmd =
            RecentTagsMenu.update localStorage msg model.recentTagsMenuModel

        { model with
              recentTagsMenuModel = recentTagsMenuModel },
        Cmd.map RecentTagsMenuMsg cmd

    | RecentSearchMenuMsg (RecentSearchMenu.Message.LoadLinks (term,
                                                               activeSection)),
      _ ->
        let msg =
            RecentSearchMenu.Message.LoadLinks(term, activeSection)

        let recentSearchMenuModel, _ =
            RecentSearchMenu.update localStorage msg model.recentSearchMenuModel

        let channelMenuModel =
            { model.channelMenuModel with
                  activeSection = activeSection }

        let recentTagsMenuModel =
            { model.recentTagsMenuModel with
                  activeSection = activeSection }

        { model with
              recentSearchMenuModel = recentSearchMenuModel
              channelMenuModel = channelMenuModel
              recentTagsMenuModel = recentTagsMenuModel },
        Cmd.ofMsg (LoadLinksBySearch(term, 0))

    | RecentSearchMenuMsg msg, _ ->
        let recentSearchMenuModel, cmd =
            RecentSearchMenu.update localStorage msg model.recentSearchMenuModel

        { model with
              recentSearchMenuModel = recentSearchMenuModel },
        Cmd.map RecentSearchMenuMsg cmd

    | LoadLinks (_, _, channelId, _), _ ->
        let channelMenuModel =
            { model.channelMenuModel with
                  activeChannelId = channelId }

        { model with
              channelId = channelId
              activeMenuSection = MenuSection.Channel
              channelMenuModel = channelMenuModel },
        Cmd.none

    | LoadLinksByTag (tag, _), _ ->
        let msg =
            RecentTagsMenu.Message.LoadLinks(tag, MenuSection.Tag)

        let recentTagsMenuModel, _ =
            RecentTagsMenu.update localStorage msg model.recentTagsMenuModel

        { model with
              tag = tag
              activeMenuSection = MenuSection.Tag
              recentTagsMenuModel = recentTagsMenuModel },
        Cmd.none

    | LoadLinksBySearch (term, _), _ ->
        let msg =
            RecentSearchMenu.Message.LoadLinks(term, MenuSection.Search)

        let recentSearchMenuModel, _ =
            RecentSearchMenu.update localStorage msg model.recentSearchMenuModel

        { model with
              term = term
              activeMenuSection = MenuSection.Search
              recentSearchMenuModel = recentSearchMenuModel },
        Cmd.none

    | UpdateChannelId channelId, _ ->
        let channelMenuModel =
            { model.channelMenuModel with
                  activeChannelId = channelId }

        { model with
              channelId = channelId
              activeMenuSection = MenuSection.Channel
              channelMenuModel = channelMenuModel },
        Cmd.none

    | ResetComponents, _ ->
        let channelMenuModel =
            { model.channelMenuModel with
                  isReady = false }

        let recentTagsMenuModel =
            { model.recentTagsMenuModel with
                  isReady = false }

        let recentSearchMenuModel =
            { model.recentSearchMenuModel with
                  isReady = false }

        { model with
              channelMenuModel = channelMenuModel
              recentTagsMenuModel = recentTagsMenuModel
              recentSearchMenuModel = recentSearchMenuModel },
        Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        match model.authState with
        | AuthState.Success ->
            let channelForm =
                ChannelMenuForm.view
                    (model.channelMenuFormModel)
                    (ChannelMenuFormMsg
                     >> dispatch)

            let channels =
                ChannelMenu.view
                    (model.channelMenuModel)
                    (ChannelMenuMsg
                     >> dispatch)

            let recentTags =
                RecentTagsMenu.view
                    (model.recentTagsMenuModel)
                    model.activeMenuSection
                    (RecentTagsMenuMsg
                     >> dispatch)

            let recentSearchMenu =
                RecentSearchMenu.view
                    (model.recentSearchMenuModel)
                    model.activeMenuSection
                    (RecentSearchMenuMsg
                     >> dispatch)

            let isActiveClass =
                match model.channelId = Guid.Empty
                      && model.activeMenuSection = Channel with
                | true -> Bulma.``is-active``
                | false -> ""

            ComponentsTemplate.MenuSidebar()
                              .LoadAllLinks(fun _ ->
                              (dispatch
                                  (LoadLinks(false, String.Empty, Guid.Empty, 0))))
                              .AllActiveClass(isActiveClass)
                              .ChannelListHole(channels)
                              .ChannelForm(channelForm)
                              .RecentTagListHole(recentTags)
                              .RecentSearchListHole(recentSearchMenu).Elt()
        | _ -> empty



let view authState (model: Model) dispatch =
    ecomp<Component, _, _> [] { model with authState = authState } dispatch
