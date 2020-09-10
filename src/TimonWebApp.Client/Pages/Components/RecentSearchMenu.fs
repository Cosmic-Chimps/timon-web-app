module TimonWebApp.Client.Pages.Components.RecentSearchMenu

open Bolero
open Bolero.Html
open System
open Elmish
open TimonWebApp.Client.Common

type Model =
    { terms: string list
      activeTerm: string
      activeSection: MenuSection }
    static member Default =
        { terms = List.empty
          activeTerm = String.Empty
          activeSection = MenuSection.Search }

type Message = LoadLinks of string

let update model msg =
    match msg with
    | LoadLinks _ -> model, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.terms (fun t ->
            let isActive =
                match model.activeTerm = t && model.activeSection = Search with
                | true -> Bulma.``is-active``
                | false -> String.Empty

            ComponentsTemplate.MenuRecentSearchItem()
                .Name(t)
                .ActiveClass(isActive)
                .LoadLinks(fun _ -> (dispatch (LoadLinks t)))
                .Elt())

let view (model: Model) (activeSection: MenuSection) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              activeSection = activeSection }
        dispatch
