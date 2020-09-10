module TimonWebApp.Client.Pages.Components.LinkViewList

open System.Net
open Bolero
open Elmish
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open System
open Bolero.Html

type TagForm = { tags: string }

type LinkViewValidationForm =
    { view: GetLinksResultProvider.Link
      isTagFormOpen: bool
      errorValidateForm: Result<TagForm, Map<string, string list>> option
      tagForm: TagForm
      openMoreInfo: bool }

type Model =
    { links: LinkViewValidationForm array
      authentication: AuthState }
    static member Default =
        { links = Array.empty
          authentication = AuthState.NotTried }

type Message =
    | SetTagFormField of string * string * LinkViewValidationForm
    | ValidateTag of LinkViewValidationForm
    | ToggleTagForm of LinkViewValidationForm
    | AddTag of LinkViewValidationForm
    | TagsUpdated of Guid * HttpStatusCode
    | NotifyTagsUpdated
    | ToggleInfoClicked of LinkViewValidationForm
    | HideInfoClicked of LinkViewValidationForm
    | LoadLinks of Guid * string
    | LoadLinksByTag of string
    | DeleteTagFromLink of string * Guid

let validateTagForm (tagForm) =
    let validateTag (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(name + " cannot be blank")
        |> validator.IsValid (fun (x: string) ->
            x.Split(",")
            |> Seq.distinct
            |> Seq.map (fun x -> x.Trim())
            |> Seq.filter (fun x -> not (String.IsNullOrEmpty(x)))
            |> Seq.length
            |> (fun z -> z > 0)) (name + " invalid format")
        |> validator.End

    all
    <| fun t -> { tags = validateTag t "Tags" tagForm.tags }

let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
    let updateLink (linkForm: LinkViewValidationForm) =
        model.links
        |> Array.tryFind (fun itLinkForm -> itLinkForm.view.Link.Id = linkForm.view.Link.Id)
        |> (fun x ->
            match x with
            | Some _ ->
                model.links
                |> Array.map (fun v -> if v.view.Link.Id = linkForm.view.Link.Id then linkForm else v)
            | None -> model.links)

    let validateTagForced (linkForm: LinkViewValidationForm) form =
        let mapResults = validateTagForm form
        { linkForm with
              tagForm = form
              errorValidateForm = Some mapResults }

    let validateTag (validateTagForm: Result<TagForm, Map<string, string list>> option)
                    (linkForm: LinkViewValidationForm)
                    form
                    =
        match validateTagForm with
        | None -> { linkForm with tagForm = form }
        | Some _ -> validateTagForced linkForm form

    match message, model with
    | SetTagFormField ("tags", value, linkForm), _ ->
        let linkViewValidationForm =
            { linkForm.tagForm with
                  tags = value.Trim() }
            |> validateTag linkForm.errorValidateForm linkForm

        let arrayLinkView = updateLink linkViewValidationForm
        { model with links = arrayLinkView }, Cmd.none

    | ValidateTag linkForm, _ ->
        let linkViewValidationForm =
            linkForm.tagForm |> validateTagForced linkForm

        let arrayLinkView = updateLink linkViewValidationForm

        { model with links = arrayLinkView },
        Cmd.ofMsg
            (AddTag
                { linkForm with
                      errorValidateForm = linkViewValidationForm.errorValidateForm })

    | TagsUpdated (linkId, _), _ ->
        let linkForm =
            model.links
            |> Array.find (fun lf -> lf.view.Link.Id = linkId)

        let tagForm = { linkForm.tagForm with tags = "" }

        let newLf =
            { linkForm with
                  tagForm = tagForm
                  isTagFormOpen = false
                  errorValidateForm = None }

        let linksMapAdding = updateLink newLf

        { model with links = linksMapAdding }, Cmd.ofMsg NotifyTagsUpdated

    | ToggleTagForm (linkForm), _ ->
        let linkForm' =
            { linkForm with
                  isTagFormOpen = not linkForm.isTagFormOpen }

        let links = updateLink linkForm'

        let inputId =
            sprintf "tag_link_box_%O" linkForm.view.Link.Id

        jsRuntime.InvokeVoidAsync("jsTimon.focusElement", inputId)
        |> ignore

        { model with links = links }, Cmd.none

    | ToggleInfoClicked (linkForm), _ ->
        let linkForm' =
            { linkForm with
                  openMoreInfo = not linkForm.openMoreInfo }

        let links = updateLink linkForm'

        { model with links = links }, Cmd.none
    | HideInfoClicked (linkForm), _ ->
        let linkForm' = { linkForm with openMoreInfo = false }
        let links = updateLink linkForm'

        { model with links = links }, Cmd.none
    | AddTag linkForm, _ ->

        let hasError =
            match linkForm.errorValidateForm with
            | Some (Error _) -> Some(model, Cmd.none)
            | _ -> None

        if hasError.IsSome then
            hasError.Value
        else
            let payload: AddTagPayload =
                { linkId = linkForm.view.Link.Id.ToString()
                  tags = linkForm.tagForm.tags }

            let cmd =
                Cmd.ofAsync addTags (timonService, payload) TagsUpdated raise

            model, cmd
    | DeleteTagFromLink (tag, linkId), _ ->
        let payload = { linkId = linkId; tagName = tag }

        let cmd =
            Cmd.ofAsync deleteTagFromLink (timonService, payload) TagsUpdated raise

        model, cmd
    | LoadLinks _, _ -> model, Cmd.none
    | LoadLinksByTag _, _ -> model, Cmd.none
    | _, _ -> model, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.links (fun l ->
            let inputId = sprintf "tag_link_box_%O" l.view.Link.Id

            let formFieldItem =
                Controls.inputAdd
                    inputId
                    "Add new tag or more separated by commas"
                    Mdi.``mdi-tag-outline``
                    l.errorValidateForm
                    None

            let inputCallback =
                fun v -> dispatch (SetTagFormField("tags", v, l))

            let buttonAction = (fun _ -> dispatch (ValidateTag l))

            let inputBox, icon =
                match l.isTagFormOpen with
                | true ->
                    (formFieldItem "Tags" l.tagForm.tags inputCallback buttonAction, Mdi.``mdi-close-circle-outline``)
                | false -> (empty, Mdi.``mdi-plus-circle-outline``)

            let iconNode =
                a [ attr.``class`` Bulma.``has-text-grey``
                    on.click (fun _ -> dispatch (ToggleTagForm l)) ] [
                    i [ attr.``class``
                        <| String.concat " " [ "mdi"; icon ] ] []
                ]

            let linkTags =
                concat [ match l.view.Link.Tags with
                         | "" -> empty
                         | _ ->
                             forEach ((l.view.Link.Tags + l.view.Data.Tags).Split(",")) (fun tag ->
                                 a [ attr.``class`` Bulma.``level-item``
                                     on.click (fun _ -> dispatch (LoadLinksByTag(tag.Trim()))) ] [
                                     span [ attr.``class``
                                            <| String.concat " " [ Bulma.tag; Bulma.``is-info`` ] ] [
                                         text (tag.Trim())
                                         button [ attr.``class``
                                                  <| String.concat " " [ Bulma.delete; Bulma.``is-small`` ]
                                                  on.click (fun _ -> dispatch (DeleteTagFromLink(tag, l.view.Link.Id))) ] []
                                     ]
                                 ])

                         iconNode ]

            let tagForm =
                match model.authentication with
                | AuthState.Success ->
                    concat
                        [ cond l.isTagFormOpen
                          <| function
                          | true -> inputBox
                          | false -> empty ]
                | _ -> empty

            let isActiveDropdownClass =
                match l.openMoreInfo with
                | true -> Bulma.``is-active``
                | false -> ""

            ComponentsTemplate.LinkItem().Url(l.view.Link.Url).Title(l.view.Link.Title)
                              .DomainName(l.view.Link.DomainName).Date(l.view.Data.DateCreated.ToString())
                              .ShortDescription(l.view.Link.ShortDescription)
                              .ChannelName(sprintf "#%s" (l.view.Channel.Name)).SharedBy(l.view.User.Email)
                              .Via(l.view.Data.Via).LinkTags(linkTags).TagForm(tagForm)
                              .OnMoreInfoClicked(fun _ -> dispatch (ToggleInfoClicked l))
                              .OnMoreInfoBlurred(fun _ -> dispatch (HideInfoClicked l))
                              .ShowDropdownClass(isActiveDropdownClass)
                              .OnChannelClicked(fun _ -> dispatch (LoadLinks(l.view.Channel.Id, l.view.Channel.Name)))
                              .Elt())

let view authState (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authentication = authState }
        dispatch
