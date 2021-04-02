module TimonWebApp.Client.Main

open System
open Blazored.LocalStorage
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Components
open TimonWebApp.Client.ClubServices
open TimonWebApp.Client.AuthServices
open TimonWebApp.Client.LinkServices
open TimonWebApp.Client.ChannelServices
open Dtos

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/init">] Start
    | [<EndPoint "/log-in">] Login of PageModel<Login.Model>
    | [<EndPoint "/sign-up">] SignUp of PageModel<SignUp.Model>
    | [<EndPoint "/">] Home of PageModel<Home.Model>

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | HomeMsg of Home.Message
    | LoginMsg of Login.Message
    | SignUpMsg of SignUp.Message
    | CommonMessage of Common.Message
    | Rendered
    | SignOutRequested
    | SignedOut
    | RemoveBuffer
    | RecvSignedInAs of string option
    | Error of exn
    | OnClubsLoaded of ClubView array

/// The Elmish application's model.
type Model =
    { page: Page
      state: State
      error: string option
      signedInAs: option<string>
      signInFailed: bool
      bufferedCommand: Cmd<Message> }

let init =
    { page = Start
      error = None
      signedInAs = None
      signInFailed = false
      bufferedCommand = Cmd.none
      state =
          { Authentication = AuthState.NotTried
            Configuration = ConfigurationState.NotInitialized } },
    Cmd.none


let initPage init (model: Model) msg page =
    let pageModel, cmd = init

    let page =
        { Model = pageModel }
        |> page

    { model with page = page }, Cmd.map msg cmd

let initLogin (jsRunTime: IJSRuntime) model =
    initPage (Login.init jsRunTime) model LoginMsg Login

let initHome (jsRunTime: IJSRuntime) model =
    initPage (Home.init jsRunTime model.state.Authentication) model HomeMsg (fun pageModel -> Home(pageModel))

let initSignUp (jsRunTime: IJSRuntime) model =
    initPage (SignUp.init jsRunTime) model SignUpMsg SignUp

let signOut (_: IJSRuntime) (timonService: TimonService) =
    let doWork () =
        async {
            do! timonService.authService.``sign-out`` ()
            return SignedOut
        }

    Cmd.OfAsync.either doWork () id raise

let update
    (jsRuntime: IJSRuntime)
    (timonService: TimonService)
    (localStorage: ILocalStorageService)
    message
    (model: Model)
    =
    // printfn "MainUpdate %s" (message.ToString())

    let genericUpdate update subModel msg msgFn pageFn =
        let subModel, cmd = update msg subModel

        { model with
              page = pageFn ({ Model = subModel }) },
        Cmd.map msgFn cmd

    match message, model.page with
    | RemoveBuffer, _ ->
        { model with
              bufferedCommand = Cmd.none },
        Cmd.none

    | Rendered, _ ->
        let cmdGetUserName =
            Cmd.OfAuthorized.either timonService.authService.``get-user-name`` () RecvSignedInAs Error

        model, cmdGetUserName

    | RecvSignedInAs option, _ ->
        let cmdSetPage = Cmd.none

        match option with
        | Some _ ->
            let state =
                { model.state with
                      Authentication = AuthState.Success }

            let m =
                { model with
                      signedInAs = option
                      state = state }

            let cmd =
                Cmd.OfAsync.either getClubs timonService OnClubsLoaded Error

            m, cmd

        | None _ -> model, cmdSetPage

    | OnClubsLoaded clubs, _ ->
        let clubHead = clubs |> Seq.tryHead

        match clubHead with
        | None ->
            { model with
                  page = Home({ Model = Home.Model.Default }) },
            Cmd.map
                HomeMsg
                (Cmd.map Home.AnonymousLinkViewListMsg (Cmd.ofMsg (AnonymousLinkViewList.Message.LoadLinks(0))))

        | Some club ->
            let searchBoxModel =
                { Home.Model.Default.searchBoxModel with
                      clubName = club.Name }

            let clubSidebarModel =
                { Home.Model.Default.clubSidebarModel with
                      clubs = clubs }

            let homeModel =
                { Home.Model.Default with
                      clubName = club.Name
                      clubId = club.Id
                      searchBoxModel = searchBoxModel
                      clubSidebarModel = clubSidebarModel }

            { model with
                  page = Home({ Model = homeModel }) },
            Cmd.map
                HomeMsg
                (Cmd.ofMsg (Home.Message.LoadClubLinks(true, club.Name, club.Id, String.Empty, Guid.Empty, 0)))

    | SignOutRequested, _ -> model, signOut jsRuntime timonService

    | SignedOut, _ ->
        let state =
            { model.state with
                  Authentication = AuthState.Failed }

        let cmdSetPage = Cmd.ofMsg (SetPage(Start))

        { model with state = state }, cmdSetPage

    | HomeMsg msg, Home (homePageModel) ->
        genericUpdate
            (Home.update jsRuntime timonService localStorage)
            (homePageModel.Model)
            msg
            HomeMsg
            (fun pm -> Home(pm))

    | LoginMsg (Login.Message.LoginSuccess authentication), Login loginModel ->
        let loginModel, _ =
            Login.update timonService (Login.Message.LoginSuccess authentication) loginModel.Model

        let model' =
            { model with
                  page = Login({ Model = loginModel }) }

        match authentication with
        | Some _ ->
            let state =
                { model'.state with
                      Authentication = AuthState.Success }

            let cmdSetPage =
                Cmd.OfAsync.either getClubs timonService OnClubsLoaded Error

            { model' with
                  signedInAs = authentication
                  state = state },
            cmdSetPage
        | None _ -> model', Cmd.none

    | LoginMsg msg, Login loginModel ->
        let m, cmd =
            Login.update timonService msg loginModel.Model

        { model with
              page = Login({ Model = m }) },
        Cmd.map LoginMsg cmd

    | SignUpMsg (SignUp.Message.SignUpSuccess authentication), SignUp signUpModel ->
        let signUpModel, _ =
            SignUp.update timonService (Pages.SignUp.Message.SignUpSuccess authentication) signUpModel.Model

        let model' =
            { model with
                  page = SignUp({ Model = signUpModel }) }

        match authentication with
        | Some _ ->
            let state =
                { model'.state with
                      Authentication = AuthState.Success }

            let cmdSetPage =
                Cmd.OfAsync.either getClubs timonService OnClubsLoaded Error

            { model' with
                  signedInAs = authentication
                  state = state },
            cmdSetPage
        | None _ -> model', Cmd.none

    | SignUpMsg msg, SignUp signUpModel ->
        let m, cmd =
            SignUp.update timonService msg signUpModel.Model

        { model with
              page = SignUp({ Model = m }) },
        Cmd.map SignUpMsg cmd

    | SetPage (Page.Login _), _ -> initLogin jsRuntime model

    | SetPage (Page.SignUp _), _ -> initSignUp jsRuntime model

    | SetPage (Page.Start), _ -> initHome jsRuntime model

    //    | SetPage page -> { model with page = page }

    // | SetPage (Page.Home (clubName, m)), _ ->
    //     { model with
    //           Page = Home(clubName, { Model = m.Model }) },
    //     Cmd.map HomeMsg (Cmd.ofMsg (Home.Message.LoadLinks(true, clubName, Guid.Empty, 0)))

    // | SetPage (Page.Home m), _ ->
    //     match m.Model.clubName with
    //     | test when test = String.Empty -> initHome  jsRuntime model
    //     | _ -> model, Cmd.none

    | _ -> model, Cmd.none


let defaultModel (jsRuntime: IJSRuntime) =
    function
    | Login model -> Router.definePageModel model Login.Model.Default
    | Home (model) -> Router.definePageModel model Home.Model.Default
    | SignUp model -> Router.definePageModel model SignUp.Model.Default
    | Start -> ()

let buildRouter (jsRuntime: IJSRuntime) =
    Router.inferWithModel SetPage (fun m -> m.page) (defaultModel jsRuntime)

type NavbarTemplate = Template<"wwwroot/navbar.html">

type NavbarEndItemsDisplay() =
    inherit ElmishComponent<Model, Message>()

    override this.View model dispatch =
        cond model.state.Authentication
        <| function
        | AuthState.NotTried
        | AuthState.Failed ->
            div [ attr.``class`` Bulma.buttons ] [
                a [ attr.``class``
                    <| String.concat " " [ Bulma.button; Bulma.``is-primary`` ]
                    attr.href "/sign-up" ] [
                    text "Sign up"
                ]
                a [ attr.``class``
                    <| String.concat " " [ Bulma.button; Bulma.``is-light`` ]
                    attr.href "/log-in" ] [
                    text "Log in"
                ]
            ]
        | AuthState.Success ->
            concat [
                div [ attr.``class``
                      <| String.concat
                          " "
                          [ Bulma.``navbar-item``
                            Bulma.``has-dropdown``
                            Bulma.``is-hoverable`` ] ] [
                    a [ attr.``class`` Bulma.``navbar-link`` ] [
                        text model.signedInAs.Value
                    ]
                    div [ attr.``class``
                          <| String.concat
                              " "
                              [ Bulma.``navbar-dropdown``
                                Bulma.``is-right`` ] ] [
                        a [ attr.``class``
                            <| String.concat " " [ Bulma.``navbar-item`` ] ] [
                            span [ attr.``class``
                                   <| String.concat " " [ Bulma.icon; Bulma.``is-small`` ] ] [
                                i [ attr.``class``
                                    <| String.concat " " [ "mdi"; Mdi.``mdi-account`` ] ] []
                            ]
                            RawHtml "&nbsp;Profile"
                        ]
                        hr [
                            attr.``class``
                            <| String.concat " " [ Bulma.``navbar-divider`` ]
                        ]
                        a [ attr.``class``
                            <| String.concat " " [ Bulma.``navbar-item`` ]
                            on.click (fun _ -> dispatch SignOutRequested) ] [
                            span [ attr.``class``
                                   <| String.concat " " [ Bulma.icon; Bulma.``is-small`` ] ] [
                                i [ attr.``class``
                                    <| String.concat " " [ "mdi"; Mdi.``mdi-logout`` ] ] []
                            ]
                            RawHtml "&nbsp;Logout"
                        ]
                    ]
                ]
            ]

let view (js: IJSRuntime) (mainModel: Model) dispatch =
    let content =
        cond mainModel.page
        <| function
        | Login (model) -> Login.view js model.Model (LoginMsg >> dispatch)
        | SignUp (model) ->
            SignUp.view
                model.Model
                (SignUpMsg
                 >> dispatch)
        | Home (model) -> Home.view mainModel.state.Authentication model.Model (HomeMsg >> dispatch)
        | Start -> empty

    let navbarEndItemsDisplay =
        ecomp<NavbarEndItemsDisplay, _, _> [] mainModel dispatch

    NavbarTemplate()
        .navbarEndItems(navbarEndItemsDisplay)
        .content(content)
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val localStorage = Unchecked.defaultof<ILocalStorageService> with get, set

    static member val Dispatchers: System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit), unit> =
        System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit), unit>() with get, set

    interface IDisposable with
        member this.Dispose() =
            MyApp.Dispatchers.TryRemove(this.Dispatch)
            |> ignore

    override this.OnAfterRenderAsync(firstRender) =
        let res =
            base.OnAfterRenderAsync(firstRender)
            |> Async.AwaitTask

        async {
            do! res

            if firstRender then
                MyApp.Dispatchers.TryAdd(this.Dispatch, ())
                |> ignore

                this.Dispatch Rendered

            return ()
        }
        |> Async.StartImmediateAsTask
        :> _

    override this.Program =
        let authService = this.Remote<AuthService>()
        let linkService = this.Remote<LinkService>()
        let channelService = this.Remote<ChannelService>()
        let clubService = this.Remote<ClubService>()

        let timonService =
            { linkService = linkService
              authService = authService
              channelService = channelService
              clubService = clubService }

        let router = buildRouter (this.JSRuntime)

        let update =
            update (this.JSRuntime) timonService this.localStorage

        Program.mkProgram (fun _ -> init) (update) (view this.JSRuntime)
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
