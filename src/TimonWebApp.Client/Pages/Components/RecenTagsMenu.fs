module TimonWebApp.Client.Pages.Components.RecentTagsMenu

open Bolero
open Bolero.Html
open System
open Elmish
open TimonWebApp.Client.Common
open Blazored.LocalStorage
open FSharp.Json

type Model =
    { tags: string list
      activeTag: string
      activeSection: MenuSection
      isReady: bool
      clubId: Guid }
    static member Default =
        { tags = List.empty
          activeTag = String.Empty
          activeSection = MenuSection.Tag
          isReady = false
          clubId = Guid.Empty }

type Message =
    | LoadLinks of string * MenuSection
    | LoadTags of Guid
    | RecentTagsUpdated of string list


let updateTagsLocalStorage ((localStorage: ILocalStorageService), (model: Model)) =
    async {
        let key =
            sprintf "timon_recent_tags_%O" model.clubId

        let! exists =
            localStorage.ContainKeyAsync(key).AsTask()
            |> Async.AwaitTask

        match exists with
        | false ->
            match model.activeTag
                  <> String.Empty with
            | false -> return []
            | true ->
                localStorage.SetItemAsync<string list>(key, [ model.activeTag ])
                    .AsTask()
                |> Async.AwaitTask
                |> ignore

                return [ model.activeTag ]
        | true ->
            let! recentTagsJson =
                localStorage.GetItemAsStringAsync(key).AsTask()
                |> Async.AwaitTask

            let recentTags = Json.deserialize (recentTagsJson)

            match model.activeTag
                  <> String.Empty with
            | false -> return recentTags
            | true ->
                return recentTags
                       |> List.tryFind (fun tag -> tag = model.activeTag)
                       |> fun found ->
                           match found with
                           | Some _ -> recentTags
                           | None _ ->
                               let recentTags' =
                                   match recentTags.Length
                                         + 1 > 9 with
                                   | false ->
                                       [ model.activeTag ]
                                       @ recentTags
                                   | true ->
                                       [ model.activeTag ]
                                       @ recentTags.[..recentTags.Length
                                                       - 2]

                               localStorage.SetItemAsync<string list>(key,
                                                                      recentTags')
                                   .AsTask()
                               |> Async.AwaitTask
                               |> ignore
                               recentTags'
    }


let update (localStorage: ILocalStorageService) msg model =
    match msg with
    | RecentTagsUpdated recentTags ->
        { model with
              tags = recentTags
              isReady = true },
        Cmd.none
    | LoadTags clubId ->
        let model' = { model with clubId = clubId }

        let cmdUpdateRecentTags =
            Cmd.OfAsync.either
                updateTagsLocalStorage
                (localStorage, model')
                RecentTagsUpdated
                raise

        model', cmdUpdateRecentTags
    | LoadLinks (tag, activeSection) ->
        { model with
              activeTag = tag
              activeSection = activeSection },
        Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.tags (fun t ->
            let isActive =
                match model.activeTag = t
                      && model.activeSection = Tag with
                | true -> Bulma.``is-active``
                | false -> String.Empty

            ComponentsTemplate.MenuTagItem().Name(t).ActiveClass(isActive)
                              .LoadLinks(fun _ ->
                              (dispatch (LoadLinks(t, MenuSection.Tag)))).Elt())

let view (model: Model) (activeSection: MenuSection) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              activeSection = activeSection }
        dispatch
