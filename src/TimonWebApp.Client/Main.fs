module TimonWebApp.Client.Main

open System
open System.Text.Json
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
    | Start
    | [<EndPoint "/">] Home of PageModel<Home.Model>
    | [<EndPoint "/login">] Login of PageModel<Login.Model>

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | HomeMsg of Home.Message
    | LoginMsg of Login.Message
    | CommonMessage of Common.Message
    | Rendered
//    | TokenRead of Authentication
//    | TokenSaved of Authentication
//    | TokenSet
//    | TokenNotFound
    | SignOutRequested
    | SignedOut
//    | ConfigurationRead of ConfigurationState
    | ConfigurationNotFound
    | RemoveBuffer
    | RecvSignedInAs of option<string>
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
    { Page = Start
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

let initHome (jsRunTime: IJSRuntime) model =
    initPage (Home.init jsRunTime) model HomeMsg Home

let signOut (jsRuntime : IJSRuntime) (timonService: TimonService)  =
    let doWork () =
        async{
            do! timonService.AuthService.``sign-out``()
            return SignedOut
        }
    Cmd.ofAsync doWork () id raise

let update (jsRuntime: IJSRuntime) (timonService: TimonService) message (model: Model) =
    jsRuntime.InvokeAsync("console.log", message.ToString()) |> ignore
    
    let genericUpdate update subModel msg msgFn pageFn =
        let subModel, cmd = update msg subModel
        { model with
              Page = pageFn ({ Model = subModel }) },
        Cmd.map msgFn cmd

    let genericUpdateWithCommon update subModel msg msgFn pageFn =
        let subModel, cmd, (commonCommand: Cmd<Common.Message>) = update msg (subModel, model.State)
        if commonCommand |> List.isEmpty then
            { model with
                  Page = pageFn ({ Model = subModel }) },
            Cmd.map msgFn cmd
        else
            let m =
                { model with
                      Page = pageFn ({ Model = subModel }) }

            m, Cmd.map CommonMessage commonCommand

    match message, model.Page with
    | RemoveBuffer, _ -> { model with BufferedCommand = Cmd.none}, Cmd.none
    
    | Rendered, _ ->
        let cmdGetUserName = Cmd.ofAuthorized
                                timonService.AuthService.``get-user-name`` ()
                                RecvSignedInAs
                                Error
                                
        let cmdLoadHome = Cmd.map HomeMsg (Cmd.ofMsg Home.Message.LoadLinks)
        let cmd = Cmd.batch[cmdGetUserName; cmdLoadHome]
        model, cmd
            

//    | RecvSignedInAs, _ ->
//        model, Cmd.ofMsg Home.Message.LoadLinks
    
    | SignOutRequested, _ ->
        model, signOut jsRuntime timonService
    
    | SignedOut, _ ->
        let state = { model.State with Authentication = AuthState.Failed }
        {model with State = state }, Cmd.ofMsg (SetPage (Home(Router.noModel)))

    | HomeMsg msg, Home (homePageModel) ->
        genericUpdate (Home.update jsRuntime timonService) (homePageModel.Model) msg HomeMsg Home
        
    | LoginMsg (Login.Message.LoginSuccess authentication), _ ->
        match authentication with
        | Some _ ->
            let state = { model.State with Authentication = AuthState.Success }
            { model with SignedInAs = authentication; State = state}, Cmd.ofMsg (SetPage (Home (Router.noModel)))
        | None _ -> model, Cmd.none
    
    | LoginMsg msg, Login loginModel ->
        let m, cmd =
            Login.update timonService msg (loginModel.Model, model.State)
        { model with Page = Login({ Model = m }) }, Cmd.map LoginMsg cmd
    
    | SetPage (Page.Login _), _ -> initLogin jsRuntime model

    | SetPage (Page.Home _), _ ->
        initHome jsRuntime model
    
    | SetPage (Start), _
    | _ -> model, Cmd.none

let defaultPageModel (jsRuntime: IJSRuntime) =
    function
    | Login model -> Router.definePageModel model (Login.init jsRuntime |> fst)
    | Home (model) ->
        Router.definePageModel model (Home.init jsRuntime |> fst)
    | Start -> ()

let buildRouter (jsRuntime: IJSRuntime) =
    Router.inferWithModel SetPage (fun m -> m.Page) (defaultPageModel jsRuntime)

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
                    <| String.concat " " [ Bulma.button; Bulma.``is-primary`` ] ] [
                    text "Sign up"
                ]
                a [ attr.``class``
                    <| String.concat " " [ Bulma.button; Bulma.``is-light`` ]
                    attr.href "/login" ] [
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

let view (js: IJSRuntime) (model: Model) dispatch =
    let content =
        cond model.Page
        <| function
        | Login (model) -> Login.view model.Model (LoginMsg >> dispatch)
        | Home (model) -> Home.view model.Model (LoginMsg >> dispatch)
        | Start -> h2 [] []

    let navbarEndItemsDisplay =
        ecomp<NavbarEndItemsDisplay, _, _> [] model dispatch

    NavbarTemplate().navbarEndItems(navbarEndItemsDisplay).content(content).Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()
    
    static member  val Dispatchers :  System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit), unit>
        = new System.Collections.Concurrent.ConcurrentDictionary<(Message -> unit),unit>() with get, set
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
        //        Program.mkSimple (fun _ -> initModel) update view
        let authService = this.Remote<AuthService>()
        let linkService = this.Remote<LinkService>()
        let timonService = {
            LinkService = linkService
            AuthService = authService
        }
        let router = buildRouter (this.JSRuntime)
        let update = update (this.JSRuntime) timonService
        Program.mkProgram (fun _ -> init) (update) (view this.JSRuntime)
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
