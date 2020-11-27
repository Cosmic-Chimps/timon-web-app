module TimonWebApp.Client.Pages.Components.RecentSearchMenu

open Bolero
open Bolero.Html
open System
open Elmish
open TimonWebApp.Client.Common
open Blazored.LocalStorage
open FSharp.Json

type Model =
    { terms: string list
      activeTerm: string
      activeSection: MenuSection
      isReady: bool
      clubId: Guid }
    static member Default =
        { terms = List.empty
          activeTerm = String.Empty
          activeSection = MenuSection.Search
          isReady = false
          clubId = Guid.Empty }

type Message =
    | LoadLinks of string * MenuSection
    | LoadTerms of Guid
    | RecentTermsUpdated of string list


let updateSearchLocalStorage ((localStorage: ILocalStorageService),
                              (model: Model)) =
    async {
        let key =
            sprintf "timon_recent_search_%O" model.clubId

        let! exists =
            localStorage.ContainKeyAsync(key).AsTask()
            |> Async.AwaitTask

        match exists with
        | false ->
            match model.activeTerm
                  <> String.Empty with
            | false -> return []
            | true ->
                localStorage.SetItemAsync<string list>(key, [ model.activeTerm ])
                    .AsTask()
                |> Async.AwaitTask
                |> ignore

                return [ model.activeTerm ]
        | true ->
            let! recentSearchJson =
                localStorage.GetItemAsStringAsync(key).AsTask()
                |> Async.AwaitTask

            let recentSearch = Json.deserialize (recentSearchJson)

            match model.activeTerm
                  <> String.Empty with
            | false -> return recentSearch
            | true ->
                return recentSearch
                       |> List.tryFind (fun tag -> tag = model.activeTerm)
                       |> fun found ->
                           match found with
                           | Some _ -> recentSearch
                           | None _ ->
                               let recentSearch' =
                                   match recentSearch.Length
                                         + 1 > 9 with
                                   | false ->
                                       [ model.activeTerm ]
                                       @ recentSearch
                                   | true ->
                                       [ model.activeTerm ]
                                       @ recentSearch.[..recentSearch.Length
                                                         - 2]

                               localStorage.SetItemAsync<string list>(key,
                                                                      recentSearch')
                                   .AsTask()
                               |> Async.AwaitTask
                               |> ignore
                               recentSearch'
    }

let update (localStorage: ILocalStorageService) msg model =
    match msg with
    | RecentTermsUpdated recentTerms ->
        { model with
              terms = recentTerms
              isReady = true },
        Cmd.none

    | LoadTerms clubId ->
        let model' =
            { model with
                  clubId = clubId
                  activeTerm = ""
                  terms = []
                  isReady = false }

        let cmdUpdateRecentTags =
            Cmd.OfAsync.either
                updateSearchLocalStorage
                (localStorage, model')
                RecentTermsUpdated
                raise

        model', cmdUpdateRecentTags

    | LoadLinks (term, activeSection) ->
        { model with
              activeTerm = term
              activeSection = activeSection },
        Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        forEach model.terms (fun t ->
            let isActive =
                match model.activeTerm = t
                      && model.activeSection = Search with
                | true -> Bulma.``is-active``
                | false -> String.Empty

            ComponentsTemplate.MenuRecentSearchItem().Name(t)
                              .ActiveClass(isActive)
                              .LoadLinks(fun _ ->
                              (dispatch (LoadLinks(t, MenuSection.Search))))
                              .Elt())

let view (model: Model) (activeSection: MenuSection) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              activeSection = activeSection }
        dispatch
