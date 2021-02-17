module TimonWebApp.Client.Pages.Components.ChannelMenu

open System
open System.ComponentModel.Design
open Bolero
open Elmish
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open Bolero.Html
open TimonWebApp.Client.Pages.Controls
open Microsoft.JSInterop
open TimonWebApp.Client.ChannelServices
open TimonWebApp.Client.ClubServices

type Model =
    { channels: ChannelView array
      authentication: AuthState
      activeChannelId: Guid
      activeSection: MenuSection
      isReady: bool
      clubId: ClubId }
    static member Default =
        { channels = Array.empty
          authentication = AuthState.NotTried
          activeChannelId = Guid.Empty
          activeSection = MenuSection.Channel
          isReady = false
          clubId = Guid.Empty }

type Message =
    | OnChannelsLoaded of ClubId * ChannelView array
    | LoadChannelLinks of ChannelId * string * MenuSection

let update  (jsRuntime: IJSRuntime)
            (timonService: TimonService)
            (message: Message)
            model =
    match message with
    | OnChannelsLoaded (clubId, channels) ->
        { model with
              channels = channels
              clubId = clubId
              isReady = true },
        Cmd.none

    | LoadChannelLinks (activeChannelId, _, activeMenuSection) ->
        { model with
              activeChannelId = activeChannelId
              activeSection = activeMenuSection },
        Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()
    override this.ShouldRender(oldModel, newModel) = oldModel <> newModel

    override _.View model dispatch =
      let renderChannelItem (channel: ChannelView) =
            let isActiveClass =
                match model.activeChannelId = channel.Id
                      && model.activeSection = Channel with
                | true -> Bulma.``is-active``
                | false -> ""

            ComponentsTemplate.ChannelItem().Name(channel.Name)
                              // .ShowChannelSettings(fun _ ->
                              //   (dispatch (OpenChannelSettings (model.clubId, channel)))
                              // )
                              .LoadLinks(fun _ ->
                              (dispatch
                                  (LoadChannelLinks
                                      (channel.Id, channel.Name, MenuSection.Channel))))
                              .ActiveClass(isActiveClass).Elt()

      let isAllChannelsSelected =
          match model.activeChannelId = Guid.Empty with
          | true -> Bulma.``is-active``
          | false -> ""

      ComponentsTemplate.ChannelSection()
                              .ChannelListHole(forEach model.channels renderChannelItem)
                              .AllActiveClass(isAllChannelsSelected)
                              .LoadAllLinks(fun _ ->
                                (dispatch
                                    (LoadChannelLinks(Guid.Empty, String.Empty, MenuSection.Channel))))
                              // .ChannelSettingsModal(channelSettingsModal)
                              .Elt()

let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
