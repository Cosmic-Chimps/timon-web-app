module TimonWebApp.Client.Pages.Components.ChannelMenu

open System
open System.ComponentModel.Design
open Bolero
open Elmish
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open Bolero.Html

type Model =
    { channels: ChannelView array
      authentication: AuthState
      activeChannelId: Guid
      activeSection: MenuSection
      isReady: bool }
    static member Default =
        { channels = Array.empty
          authentication = AuthState.NotTried
          activeChannelId = Guid.Empty
          activeSection = MenuSection.Channel
          isReady = false }

type Message =
    | OnChannelsLoaded of ChannelView array
    | LoadChannelLinks of ChannelId * string * MenuSection

let update (message: Message) model =
    match message with
    | OnChannelsLoaded channels ->
        { model with
              channels = channels
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
        forEach model.channels (fun l ->
            let isActiveClass =
                match model.activeChannelId = l.Id
                      && model.activeSection = Channel with
                | true -> Bulma.``is-active``
                | false -> ""

            ComponentsTemplate.ChannelItem().Name(l.Name)
                              .LoadLinks(fun _ ->
                              (dispatch
                                  (LoadChannelLinks
                                      (l.Id, l.Name, MenuSection.Channel))))
                              .ActiveClass(isActiveClass).Elt())

let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
