module TimonWebApp.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.JSInterop
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client
open TimonWebApp.Client.Common

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/log-in">] Login of PageModel<Login.Model>
    | [<EndPoint "/sign-up">] SignUp of PageModel<SignUp.Model>
    | [<EndPoint "/{channelName}">] Home of channelName: string * model: PageModel<Home.Model>

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

/// The Elmish application's model.
type Model =
    { Page: Page
      State: State
      Error: string option
      SignedInAs: option<string>
      SignInFailed: bool
      BufferedCommand: Cmd<Message> }

let init =
    { Page = Home("", { Model = Home.Model.Default })
      Error = None
      SignedInAs = None
      SignInFailed = false
      BufferedCommand = Cmd.none
      State = {
          Authentication = AuthState.NotTried
          Configuration = ConfigurationState.NotInitialized
      }
    },
    Cmd.none


let initPage init (model: Model) msg page =
    let pageModel, cmd = init
    let page = { Model = pageModel } |> page
    { model with Page = page }, Cmd.map msg cmd

let initLogin (jsRunTime: IJSRuntime) model =
    initPage (Login.init jsRunTime) model LoginMsg Login

let initHome (jsRunTime: IJSRuntime) channel model =
    initPage (Home.init jsRunTime channel) model HomeMsg (fun pageModel -> Home(channel, pageModel))

let initSignUp (jsRunTime: IJSRuntime) model =
    initPage (SignUp.init jsRunTime) model SignUpMsg SignUp

let signOut (_ : IJSRuntime) (timonService: TimonService)  =
    let doWork () =
        async{
            do! timonService.AuthService.``sign-out``()
            return SignedOut
        }
    Cmd.ofAsync doWork () id raise

let update (jsRuntime: IJSRuntime) (timonService: TimonService) message (model: Model) =
    printfn "MainUpdate %s" (message.ToString())

    let genericUpdate update subModel msg msgFn pageFn =
        let subModel, cmd = update msg subModel
        { model with
              Page = pageFn ({ Model = subModel }) },
        Cmd.map msgFn cmd

//    let genericUpdateWithCommon update subModel msg msgFn pageFn =
//        let subModel, cmd, (commonCommand: Cmd<Common.Message>) = update msg (subModel, model.State)
//        if commonCommand |> List.isEmpty then
//            { model with
//                  Page = pageFn ({ Model = subModel }) },
//            Cmd.map msgFn cmd
//        else
//            let m =
//                { model with
//                      Page = pageFn ({ Model = subModel }) }
//
//            m, Cmd.map CommonMessage commonCommand

    match message, model.Page with
    | RemoveBuffer, _ -> { model with BufferedCommand = Cmd.none}, Cmd.none

    | Rendered, _ ->
        let cmdGetUserName = Cmd.ofAuthorized
                                timonService.AuthService.``get-user-name`` ()
                                RecvSignedInAs
                                Error
        model, cmdGetUserName

    | RecvSignedInAs option, _ ->
//        let cmdSetPage =  Cmd.map HomeMsg (Cmd.ofMsg (Home.Message.LoadLinks (true, "")))
        let cmdSetPage =  Cmd.none
        match option with
        | Some _ ->
            let state = { model.State with Authentication = AuthState.Success }
            let m = { model with SignedInAs = option; State = state}

            match model.Page with
            | Home _ -> m, Cmd.none
            | _ -> m, cmdSetPage

        | None _ -> model, cmdSetPage

    | SignOutRequested, _ ->
        model, signOut jsRuntime timonService

    | SignedOut, _ ->
        let state = { model.State with Authentication = AuthState.Failed }
        let cmdSetPage =  Cmd.ofMsg (SetPage (Home("", Router.noModel)))
        {model with State = state }, cmdSetPage

    | HomeMsg msg, Home (channel, homePageModel) ->
        genericUpdate (Home.update jsRuntime timonService) (homePageModel.Model) msg HomeMsg ( fun pm -> Home(channel, pm) )

    | LoginMsg (Login.Message.LoginSuccess authentication), _ ->
        match authentication with
        | Some _ ->
            let state = { model.State with Authentication = AuthState.Success }
            let cmdSetPage = Cmd.ofMsg (SetPage (Home ("", Router.noModel)))
            { model with SignedInAs = authentication; State = state}, cmdSetPage
        | None _ -> model, Cmd.none

    | LoginMsg msg, Login loginModel ->
        let m, cmd =
            Login.update timonService msg (loginModel.Model, model.State)
        { model with Page = Login({ Model = m }) }, Cmd.map LoginMsg cmd

    | SignUpMsg (SignUp.Message.SignUpSuccess authentication), _ ->
        match authentication with
        | Some _ ->
            let state = { model.State with Authentication = AuthState.Success }
            let cmdSetPage = Cmd.ofMsg (SetPage (Home ("", Router.noModel)))
            { model with SignedInAs = authentication; State = state}, cmdSetPage
        | None _ -> model, Cmd.none

    | SignUpMsg msg, SignUp signUpModel ->
        let m, cmd = SignUp.update timonService msg signUpModel.Model
        { model with Page  = SignUp({Model = m})}, Cmd.map SignUpMsg cmd

    | SetPage (Page.Login _), _ -> initLogin jsRuntime model

    | SetPage (Page.SignUp _), _ -> initSignUp jsRuntime model

//    | SetPage page -> { model with page = page }

    | SetPage (Page.Home (channel, m)), _ ->
        { model with Page = Home(channel, { Model = m.Model })},
            Cmd.map HomeMsg (Cmd.ofMsg (Home.Message.LoadLinks (true, channel, Guid.Empty, 0)))

    | _ -> model, Cmd.none


let defaultModel (jsRuntime: IJSRuntime) = function
    | Login model -> Router.definePageModel model Login.Model.Default
    | Home (channel, model) ->
        Router.definePageModel model { Home.Model.Default with channel = channel }
    | SignUp model -> Router.definePageModel model SignUp.Model.Default

let buildRouter (jsRuntime: IJSRuntime) =
    Router.inferWithModel SetPage (fun m -> m.Page) (defaultModel jsRuntime)

type NavbarTemplate = Template<"wwwroot/navbar.html">

type NavbarEndItemsDisplay() =
    inherit ElmishComponent<Model, Message>()

    override this.View model dispatch =
        cond model.State.Authentication
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
            concat
                [ div [attr.``class`` <| String.concat " " [ Bulma.``navbar-item``; Bulma.``has-dropdown``; Bulma.``is-hoverable`` ]] [
                    a [ attr.``class`` Bulma.``navbar-link`` ] [
                        text model.SignedInAs.Value
                    ]
                    div [attr.``class`` <| String.concat " " [ Bulma.``navbar-dropdown``; Bulma.``is-right`` ]] [
                        a [ attr.``class`` <| String.concat " " [ Bulma.``navbar-item`` ] ] [
                            span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small`` ] ] [
                                i [ attr.``class``<| String.concat " " [ "mdi"; Mdi.``mdi-account`` ] ] []
                            ]
                            RawHtml "&nbsp;Profile"
                        ]
                        hr [ attr.``class`` <| String.concat " " [ Bulma.``navbar-divider`` ] ]
                        a [ attr.``class`` <| String.concat " " [ Bulma.``navbar-item`` ]
                            on.click (fun _ -> dispatch SignOutRequested) ] [
                            span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small`` ] ] [
                                i [ attr.``class``<| String.concat " " [ "mdi"; Mdi.``mdi-logout`` ] ] []
                            ]
                            RawHtml "&nbsp;Logout"
                        ]
                    ]
                  ] ]

let view (js: IJSRuntime) (mainModel: Model) dispatch =
    let content =
        cond mainModel.Page
        <| function
        | Login (model) -> Login.view js model.Model (LoginMsg >> dispatch)
        | SignUp (model) -> SignUp.view model.Model (SignUpMsg >> dispatch)
        | Home (_, model) -> Home.view mainModel.State.Authentication model.Model (HomeMsg >> dispatch)

    let navbarEndItemsDisplay =
        ecomp<NavbarEndItemsDisplay, _, _> [] mainModel dispatch

    NavbarTemplate().navbarEndItems(navbarEndItemsDisplay).content(content).Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()
    static member  val Dispatchers :  System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit), unit>
        = System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit),unit>() with get, set
    interface IDisposable with
          member this.Dispose() =
            MyApp.Dispatchers.TryRemove(this.Dispatch) |> ignore

    override this.OnAfterRenderAsync(firstRender) =
        let res = base.OnAfterRenderAsync(firstRender) |> Async.AwaitTask
        async{
            do! res
            if firstRender then
               MyApp.Dispatchers.TryAdd(this.Dispatch,()) |> ignore
               this.Dispatch Rendered
            return ()
         }|> Async.StartImmediateAsTask :> _

    override this.Program =
        let authService = this.Remote<AuthService>()
        let linkService = this.Remote<LinkService>()
        let channelService = this.Remote<ChannelService>()
        let timonService = {
            LinkService = linkService
            AuthService = authService
            ChannelService = channelService
        }
        let router = buildRouter (this.JSRuntime)
        let update = update (this.JSRuntime) timonService
        Program.mkProgram (fun _ -> init) (update) (view this.JSRuntime)
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
