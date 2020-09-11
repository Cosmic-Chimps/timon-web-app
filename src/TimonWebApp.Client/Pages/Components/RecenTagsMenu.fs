module TimonWebApp.Client.Pages.Components.RecentTagsMenu

open Bolero
open Bolero.Html
open System
open Elmish
open TimonWebApp.Client.Common

type Model =
    { tags: string list
      activeTag: string
      activeSection: MenuSection }
    static member Default =
        { tags = List.empty
          activeTag = String.Empty
          activeSection = MenuSection.Tag }

type Message = LoadLinks of string

let update model msg =
    match msg with
    | LoadLinks _ -> model, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.tags (fun t ->
            let isActive =
                match model.activeTag = t && model.activeSection = Tag with
                | true -> Bulma.``is-active``
                | false -> String.Empty

            ComponentsTemplate.MenuTagItem().Name(t).ActiveClass(isActive).LoadLinks(fun _ -> (dispatch (LoadLinks t)))
                              .Elt())

let view (model: Model) (activeSection: MenuSection) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              activeSection = activeSection }
        dispatch