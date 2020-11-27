module TimonWebApp.Client.Pages.Components.ClubLinkViewList

open System.Net
open Bolero
open Elmish
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Controls.InputsHtml
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open System
open Bolero.Html

type TagForm = { tags: string }

type ClubLinkViewValidationForm =
    { view: GetClubLinksResultProvider.Link
      isTagFormOpen: bool
      errorValidateForm: Result<TagForm, Map<string, string list>> option
      tagForm: TagForm }

type Model =
    { links: ClubLinkViewValidationForm array
      authentication: AuthState
      isSaving: bool
      isLoading: bool
      clubId: ClubId
      isReady: bool }
    static member Default =
        { links = Array.empty
          authentication = AuthState.NotTried
          isSaving = false
          isLoading = true
          clubId = Guid.Empty
          isReady = false }

type ClubLoadListParams =
    bool * ClubName * ClubId * ChannelName * ChannelId * int

type ClubLoadListByTermParams = ClubId * string * int

type Message =
    | SetTagFormField of string * string * ClubLinkViewValidationForm
    | ValidateTag of ClubLinkViewValidationForm
    | ToggleTagForm of ClubLinkViewValidationForm
    | AddTag of ClubLinkViewValidationForm
    | TagsUpdated of Guid * HttpStatusCode
    | NotifyTagsUpdated
    | LoadLinks of Guid * string
    | LoadLinksByTag of string
    | LoadLinksSearch of string
    | DeleteTagFromLink of string * Guid
    | LoadClubLinks of ClubLoadListParams
    | LoadClubLinksByTag of ClubLoadListByTermParams
    | LoadClubLinksBySearch of ClubLoadListByTermParams
    | ClubLinksLoaded of ClubListView

let private validateTagForm (tagForm) =
    let validateTag (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank
            (name
             + " cannot be blank")
        |> validator.IsValid (fun (x: string) ->
            x.Split(",")
            |> Seq.distinct
            |> Seq.map (fun x -> x.Trim())
            |> Seq.filter (fun x -> not (String.IsNullOrEmpty(x)))
            |> Seq.length
            |> (fun z -> z > 0))
               (name
                + " invalid format")
        |> validator.End

    all
    <| fun t -> { tags = validateTag t "Tags" tagForm.tags }

let private mapClubLinksToView (dataLinks: ClubListView) =
    dataLinks.Links
    |> Seq.map (fun lv ->
        { view = lv
          isTagFormOpen = false
          errorValidateForm = None
          tagForm = { tags = "" } })
    |> Seq.toArray

let update (jsRuntime: IJSRuntime)
           (timonService: TimonService)
           (message: Message)
           (model: Model)
           =
    let updateLink (linkForm: ClubLinkViewValidationForm) =
        model.links
        |> Array.tryFind (fun itLinkForm ->
            itLinkForm.view.Link.Id = linkForm.view.Link.Id)
        |> (fun x ->
            match x with
            | Some _ ->
                model.links
                |> Array.map (fun v ->
                    if v.view.Link.Id = linkForm.view.Link.Id then
                        linkForm
                    else
                        v)
            | None -> model.links)

    let validateTagForced (linkForm: ClubLinkViewValidationForm) form =
        let mapResults = validateTagForm form
        { linkForm with
              tagForm = form
              errorValidateForm = Some mapResults }

    let validateTag (validateTagForm: Result<TagForm, Map<string, string list>> option)
                    (linkForm: ClubLinkViewValidationForm)
                    form
                    =
        match validateTagForm with
        | None -> { linkForm with tagForm = form }
        | Some _ -> validateTagForced linkForm form

    match message, model with
    | ClubLinksLoaded data, _ ->
        let linkViewFormList = mapClubLinksToView data

        { model with
              links = linkViewFormList
              isReady = true },
        Cmd.none

    | LoadClubLinks (arg: ClubLoadListParams), _ ->

        let _, _, clubId, _, channelId, page = arg

        let queryParams: GetClubLinkParams =
            { clubId = clubId
              channelId = channelId
              page = page }

        let cmd =
            Cmd.OfAsync.either
                getClubLinks
                (timonService, queryParams)
                ClubLinksLoaded
                raise

        { model with
              isLoading = true
              clubId = clubId },
        cmd

    | LoadClubLinksByTag (arg: ClubLoadListByTermParams), _ ->

        let clubId, tag, page = arg

        let queryParams: GetClubLinkByTagsParams =
            { clubId = clubId
              tagName = tag
              page = page }

        let cmd =
            Cmd.OfAsync.either
                getClubLinksByTag
                (timonService, queryParams)
                ClubLinksLoaded
                raise

        { model with
              isLoading = true
              clubId = clubId },
        cmd

    | LoadClubLinksBySearch (arg: ClubLoadListByTermParams), _ ->

        let clubId, term, page = arg

        let queryParams: GetClubLinkSearchParams =
            { clubId = clubId
              term = term
              page = page }

        let cmd =
            Cmd.OfAsync.either
                searchClubLinks
                (timonService, queryParams)
                ClubLinksLoaded
                raise

        { model with
              isLoading = true
              clubId = clubId },
        cmd

    | SetTagFormField ("tags", value, linkForm), _ ->
        let ClublinkViewValidationForm =
            { linkForm.tagForm with
                  tags = value.Trim() }
            |> validateTag linkForm.errorValidateForm linkForm

        let arrayLinkView = updateLink ClublinkViewValidationForm
        { model with links = arrayLinkView }, Cmd.none

    | ValidateTag linkForm, _ ->
        let ClublinkViewValidationForm =
            linkForm.tagForm
            |> validateTagForced linkForm

        let arrayLinkView = updateLink ClublinkViewValidationForm

        { model with links = arrayLinkView },
        Cmd.ofMsg
            (AddTag
                { linkForm with
                      errorValidateForm =
                          ClublinkViewValidationForm.errorValidateForm })

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

        { model with
              links = linksMapAdding
              isSaving = false },
        Cmd.ofMsg NotifyTagsUpdated

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
                  tags = linkForm.tagForm.tags
                  clubId = model.clubId }

            let cmd =
                Cmd.OfAsync.either
                    addTags
                    (timonService, payload)
                    TagsUpdated
                    raise

            { model with isSaving = true }, cmd
    | DeleteTagFromLink (tag, linkId), _ ->
        let payload =
            { linkId = linkId
              tagName = tag
              clubId = model.clubId }

        let cmd =
            Cmd.OfAsync.either
                deleteTagFromLink
                (timonService, payload)
                TagsUpdated
                raise

        model, cmd
    | LoadLinks _, _ -> model, Cmd.none
    | LoadLinksByTag _, _ -> model, Cmd.none
    | LoadLinksSearch _, _ -> model, Cmd.none
    | _, _ -> model, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        match model.authentication with
        | AuthState.Success ->
            forEach model.links (fun l ->
                let inputId = sprintf "tag_link_box_%O" l.view.Link.Id

                let formFieldItem =
                    inputAdd
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
                        (formFieldItem
                            "Tags"
                             l.tagForm.tags
                             inputCallback
                             buttonAction,
                         Mdi.``mdi-close-circle-outline``)
                    | false -> (empty, Mdi.``mdi-plus-circle-outline``)

                let iconNode =
                    a [ attr.``class`` Bulma.``has-text-grey``
                        on.click (fun _ -> dispatch (ToggleTagForm l)) ] [
                        i [ attr.``class``
                            <| String.concat " " [ "mdi"; icon ] ] []
                    ]

                let isLoadingButton =
                    match model.isSaving with
                    | true -> Bulma.``is-loading``
                    | false -> String.Empty

                let linkTags =
                    concat [
                        match l.view.Link.Tags with
                        | "" -> empty
                        | _ ->
                            forEach
                                ((l.view.Link.Tags
                                  + l.view.Data.Tags).Split(",")) (fun tag ->
                                a [ attr.``class`` Bulma.``level-item``
                                    on.click (fun _ ->
                                        dispatch (LoadLinksByTag(tag.Trim()))) ] [
                                    span [ attr.``class``
                                           <| String.concat
                                               " "
                                                  [ Bulma.tag
                                                    Bulma.``is-info``
                                                    isLoadingButton ] ] [
                                        text (tag.Trim())
                                    ]
                                ])
                        match l.view.CustomTags with
                        | "" -> empty
                        | _ ->
                            forEach ((l.view.CustomTags).Split(",")) (fun tag ->
                                a [ attr.``class`` Bulma.``level-item``
                                    on.click (fun _ ->
                                        dispatch (LoadLinksByTag(tag.Trim()))) ] [
                                    span [ attr.``class``
                                           <| String.concat
                                               " "
                                                  [ Bulma.tag
                                                    Bulma.``is-info``
                                                    isLoadingButton ] ] [
                                        text (tag.Trim())
                                        button [ attr.``class``
                                                 <| String.concat
                                                     " "
                                                        [ Bulma.delete
                                                          Bulma.``is-small`` ]
                                                 on.click (fun _ ->
                                                     dispatch
                                                         (DeleteTagFromLink
                                                             (tag,
                                                              l.view.Link.Id))) ] []
                                    ]
                                ])

                        iconNode
                    ]

                let tagForm =
                    match model.authentication with
                    | AuthState.Success ->
                        concat [
                            cond l.isTagFormOpen
                            <| function
                            | true -> inputBox
                            | false -> empty
                        ]
                    | _ -> empty

                let linkMetadataHole =
                    ComponentsTemplate.LinkMetadataLevel()
                                      .DomainName(l.view.Link.DomainName)
                                      .Date(l.view.Data.DateCreated.DateTime.ToString
                                                ()).Via(l.view.Data.Via)
                                      .SharedBy(l.view.User.DisplayName)
                                      .OnViaClicked(fun _ ->
                                      dispatch
                                          (LoadLinksSearch(l.view.Data.Via)))
                                      .OnDomainClicked(fun _ ->
                                      dispatch
                                          (LoadLinksSearch
                                              (l.view.Link.DomainName))).Elt()


                ComponentsTemplate.LinkItem().Url(l.view.Link.Url)
                                  .Title(l.view.Link.Title)
                                  .ShortDescription(l.view.Link.ShortDescription)
                                  .ChannelName(sprintf
                                                   "#%s"
                                                   (l.view.Channel.Name))
                                  .TagForm(tagForm).LinkTags(linkTags)
                                  .LinkMetadataHole(linkMetadataHole)
                                  .OnChannelClicked(fun _ ->
                                  dispatch
                                      (LoadLinks
                                          (l.view.Channel.Id,
                                           l.view.Channel.Name))).Elt())
        | _ -> empty

let view authState (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authentication = authState }
        dispatch
