module TimonWebApp.Client.Pages.Components.LinkViewList

open Bolero
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open Bolero.Html

type TagForm = { tags: string }

type LinkViewValidationForm =
    { view: GetLinksResultProvider.Link
      isTagFormOpen: bool
      errorValidateForm: Result<TagForm, Map<string, string list>> option
      tagForm: TagForm }

type Model =
    { links: LinkViewValidationForm array }
    static member Default =
        { links = Array.empty }

type Message =
    | Empty


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.links (fun l ->
            let linkTags =
                match l.view.Tags with
                 | "" -> empty
                 | _ ->
                     forEach (l.view.Tags.Split(",")) (fun tag ->
                        span [ attr.``class``
                                     <| String.concat
                                         " "
                                            [ Bulma.tag
                                              Bulma.``is-info``] ] [
                                  text (tag.Trim())
                                 ])

            ComponentsTemplate
                .LinkItem()
                .Url(l.view.Url)
                .Title(l.view.Title)
                .LinkTags(linkTags)
                .ShortDescription(l.view.ShortDescription)
                .Elt())

let view (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        model
        dispatch
