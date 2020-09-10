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
      activeSection: MenuSection }
    static member Default =
        { channels = Array.empty
          authentication = AuthState.NotTried
          activeChannelId = Guid.Empty
          activeSection = MenuSection.Channel }

type Message =
    | LoadLinks of Guid * string
    | Empty

let update model msg =
    match msg with
    | LoadLinks _ -> model, Cmd.none
    | Empty -> model, Cmd.none

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

            ComponentsTemplate.ChannelItem().Name(l.Name).LoadLinks(fun _ -> (dispatch (LoadLinks(l.Id, l.Name))))
                              .ActiveClass(isActiveClass).Elt())

let view authState (model: Model) (activeSection: MenuSection) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authentication = authState
              activeSection = activeSection }
        dispatch
