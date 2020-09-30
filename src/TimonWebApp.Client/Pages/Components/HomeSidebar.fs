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


type Model =
    {
      channelMenuModel: ChannelMenu.Model
      channelMenuFormModel: ChannelMenuForm.Model
      recentTagsMenuModel: RecentTagsMenu.Model
      recentSearchMenuModel: RecentSearchMenu.Model
      activeMenuSection: MenuSection
      channelId: ChannelId
      clubId: ClubId
      authState: AuthState
    }
    static member Default = {
      channelMenuModel = ChannelMenu.Model.Default
      channelMenuFormModel = ChannelMenuForm.Model.Default
      recentTagsMenuModel = RecentTagsMenu.Model.Default
      recentSearchMenuModel = RecentSearchMenu.Model.Default
      activeMenuSection = MenuSection.Channel
      channelId = Guid.Empty
      authState = AuthState.NotTried
      clubId = Guid.Empty
    }

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

let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
  match message, model with
  | LoadChannels, _ ->
    let cmd =
      Cmd.OfAsync.either getChannels (timonService, model.clubId) ChannelsLoaded raise

    let channelMenuFormModel = { model.channelMenuFormModel with clubId = model.clubId }

    { model with channelMenuFormModel = channelMenuFormModel}, cmd

  | ChannelsLoaded channels, _ ->
      let msg = ChannelMenu.Message.OnChannelsLoaded channels

      let channelMenuModel, cmd = ChannelMenu.update msg model.channelMenuModel

      let cmds = [ cmd; Cmd.none ]

      { model with channelMenuModel = channelMenuModel }, Cmd.batch cmds

  | ChannelMenuFormMsg (ChannelMenuForm.Message.NotifyChannelAdded), _ -> model, Cmd.ofMsg LoadChannels

  | ChannelMenuFormMsg msg, _ ->
    let m, cmd =
        ChannelMenuForm.update jsRuntime timonService msg model.channelMenuFormModel

    { model with channelMenuFormModel = m }, Cmd.map ChannelMenuFormMsg cmd

  | ChannelMenuMsg (ChannelMenu.Message.LoadLinks (channelId, channel, activeSection)), _ ->
    let msg =
      ChannelMenu.Message.LoadLinks (channelId, channel, activeSection)

    let channelMenuModel, cmd =
      ChannelMenu.update msg model.channelMenuModel

    let cmdBatch =
      [ cmd; Cmd.ofMsg (LoadLinks(false, channel, channelId, 0)) ]

    { model with channelMenuModel = channelMenuModel }, Cmd.batch cmdBatch

  | ChannelMenuMsg _, _ -> model, Cmd.none

  | RecentTagsMenuMsg (RecentTagsMenu.Message.LoadLinks (tag)), _ ->
      let channelModel =
          { model.channelMenuModel with
                activeChannelId = Guid.Empty
                activeSection = Tag }

      { model with channelMenuModel = channelModel }, Cmd.ofMsg (LoadLinksByTag(tag, 0))

  | RecentSearchMenuMsg (RecentSearchMenu.Message.LoadLinks (term)), _ ->
      let recentSearchModel =
          { model.recentSearchMenuModel with
                activeTerm = term
                activeSection = Search }

      { model with
            recentSearchMenuModel = recentSearchModel },
      Cmd.ofMsg (LoadLinksBySearch(term, 0))

  | LoadLinks (_, _, channelId, _), _ ->
    { model with channelId = channelId } , Cmd.none

  | LoadLinksByTag _, _ ->
    model, Cmd.none

  | LoadLinksBySearch _, _ ->
    model, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
      match model.authState with
      | AuthState.Success ->
        let channelForm =
          ChannelMenuForm.view (model.channelMenuFormModel) (ChannelMenuFormMsg >> dispatch)

        let channels =
          ChannelMenu.view (model.channelMenuModel) (ChannelMenuMsg >> dispatch)

        let recentTags =
          RecentTagsMenu.view (model.recentTagsMenuModel) model.activeMenuSection (RecentTagsMenuMsg >> dispatch)


        let recentSearchMenu =
          RecentSearchMenu.view (model.recentSearchMenuModel) model.activeMenuSection (RecentSearchMenuMsg >> dispatch)

        let isActiveClass =
          match model.channelId = Guid.Empty
                && model.activeMenuSection = Channel with
          | true -> Bulma.``is-active``
          | false -> ""

        ComponentsTemplate
          .MenuSidebar()
          .LoadAllLinks(fun _ -> (dispatch (LoadLinks(false, String.Empty, Guid.Empty, 0))))
          .AllActiveClass(isActiveClass)
          .ChannelListHole(channels)
          .ChannelForm(channelForm)
          .RecentTagListHole(recentTags)
          .RecentSearchListHole(recentSearchMenu)
          .Elt()
      | _ -> empty



let view authState (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authState = authState }
        dispatch
