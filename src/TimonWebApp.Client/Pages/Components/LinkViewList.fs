module TimonWebApp.Client.Pages.Components.AnonymousLinkViewList

open Bolero
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open Bolero.Html
open Microsoft.JSInterop
open Elmish

type TagForm = { tags: string }

type LinkView = { view: GetLinksResultProvider.Link }

type Model =
    { links: LinkView array
      isReady: bool }
    static member Default = { links = Array.empty; isReady = false }

type Message =
    | LinksLoaded of GetLinksResult
    | LoadLinks of int

let mapDataLinksToView (dataLinks: GetLinksResult) =
    dataLinks.Links
    |> Seq.map (fun lv -> { view = lv })
    |> Seq.toArray

let update (jsRuntime: IJSRuntime) (timonService: TimonService) message model =
    match message, model with
    | LinksLoaded data, _ ->
        let linkViewFormList = mapDataLinksToView data

        jsRuntime.InvokeVoidAsync("scroll", 0, 0).AsTask()
        |> Async.AwaitTask
        |> ignore

        { model with
              links = linkViewFormList
              isReady = true },
        Cmd.none

    | LoadLinks (page), _ ->
        let queryParams: GetLinkParams = { page = page }

        let linksCmd =
            Cmd.OfAsync.either
                getLinks
                (timonService, queryParams)
                LinksLoaded
                raise

        model, linksCmd

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
                                      [ Bulma.tag; Bulma.``is-info`` ] ] [
                            text (tag.Trim())
                        ])

            ComponentsTemplate.LinkItem().Url(l.view.Url).Title(l.view.Title)
                              .LinkTags(linkTags)
                              .ShortDescription(l.view.ShortDescription).Elt())

let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
