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
    | TokenRead of Common.Authentication
    | TokenSaved of Common.Authentication
    | TokenSet
    | TokenNotFound
    | SignOutRequested
    | SignedOut
    | ConfigurationRead of Common.ConfigurationState
    | ConfigurationNotFound
    | RemoveBuffer

/// The Elmish application's model.
type Model =
    { Page: Page
      State: Common.State
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
          Authentication = Common.AuthState.NotTried
          Configuration = Common.ConfigurationState.NotInitialized
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
    initPage (Home.init jsRunTime model.State.Configuration) model HomeMsg Home

let loadConfiguration (remote: AuthService) model =
    let doWork() =
        async {
            let! config = remote.``get-config``()
            return Common.ConfigurationState.Success config |> ConfigurationRead
        }
    Cmd.ofAsync doWork () id (fun _ -> ConfigurationNotFound)

let getToken (jsRuntime : IJSRuntime)  =
    let doWork () =
        async{
            let! res =
                jsRuntime.InvokeAsync<string>("getCookie", "token")
                    .AsTask()
                    |> Async.AwaitTask
            return
                match res with
                | null -> TokenNotFound
                | t ->
                    t
                    |> System.Net.WebUtility.UrlDecode
                    |> JsonSerializer.Deserialize<Common.Authentication> |> TokenRead
        }
    Cmd.ofAsync doWork () id (fun _ -> TokenNotFound)
    
let setToken (jsRuntime : IJSRuntime) (token : Common.Authentication)  =
    let doWork () =
        async{
            let ser = JsonSerializer.Serialize(token)
            do!
                jsRuntime.InvokeVoidAsync("setCookie", "token" , System.Net.WebUtility.UrlEncode(ser) , 7)
                    .AsTask()
                    |> Async.AwaitTask
            return TokenSaved token
        }
    Cmd.ofAsync doWork () id raise

let signOut (jsRuntime : IJSRuntime)  =
    let doWork () =
        async{
            do!
                jsRuntime.InvokeVoidAsync("eraseCookie", "token")
                             .AsTask()
                             |> Async.AwaitTask
            return SignedOut
        }
    Cmd.ofAsync doWork () id raise

let update (jsRuntime: IJSRuntime) remote message (model: Model) =
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
        model, loadConfiguration remote model
    
    | ConfigurationRead config, _ ->
        let state = { model.State with Configuration = config }
        let m = { model with State = state }
        m, getToken jsRuntime
    
    | SignOutRequested, _ ->
        model, signOut jsRuntime
    
    | SignedOut, _ ->
        let state = { model.State with Authentication = Common.AuthState.Failed }
        {model with State = state }, Cmd.ofMsg (SetPage (Home(Router.noModel)))
        
//        (fun pageModel -> Home(model.State.Configuration, pageModel))
//    | HomeMsg _, Home (homePageModel)
//        when (box homePageModel.Model |> function null -> true | _ -> false) ->
//            jsRuntime.InvokeAsync("console.log", "home.ignore") |> ignore
//            model, Cmd.none
    
    | HomeMsg msg, Home (homePageModel) ->
//        genericUpdate Home.update (homePageModel.Model) msg HomeMsg Home
//        let m, cmd = Home.update msg homeModel
//        genericUpdate (Home.update)(homeModel.Model) msg HomeMsg Home
        genericUpdate (Home.update jsRuntime remote) (homePageModel.Model) msg HomeMsg Home
//        jsRuntime.InvokeAsync("console.log", "home.update") |> ignore
//        jsRuntime.InvokeAsync("console.log", homePageModel) |> ignore
//        let m, cmd = Home.update jsRuntime remote msg homePageModel.Model
//        jsRuntime.InvokeAsync("console.log", cmd.Head) |> ignore
//        { model with
//              Page = Home({ Model = m }) },
//        Cmd.map HomeMsg cmd
    
    | LoginMsg (Login.Message.LoginSuccess authentication), _ ->
        match authentication with
        | Some auth ->
            model, setToken jsRuntime auth
        | None _ -> model, Cmd.ofMsg TokenNotFound
    
    | LoginMsg msg, Login loginModel ->
        let m, cmd =
            Login.update remote msg (loginModel.Model, model.State)

        { model with
              Page = Login({ Model = m }) },
        Cmd.map LoginMsg cmd
    
    | TokenRead auth, _ ->
        { model with
                  State =
                      { model.State with
                            Authentication = Common.AuthState.Success auth } },
                Cmd.ofMsg (SetPage (Home (Router.noModel)))
    
    | TokenSaved auth, _ ->
        { model with
                  State =
                      { model.State with
                            Authentication = Common.AuthState.Success auth } },
            Cmd.batch[ model.BufferedCommand; Cmd.ofMsg(RemoveBuffer)]
    
    | TokenSet , _ -> model, Cmd.batch[ model.BufferedCommand; Cmd.ofMsg(RemoveBuffer)]
    
    | TokenNotFound, _ ->
        model, Cmd.ofMsg (SetPage (Home (Router.noModel)))
//        model, Cmd.batch[ model.BufferedCommand; Cmd.ofMsg(RemoveBuffer)]

    | SetPage (Page.Login _), _ -> initLogin jsRuntime model
    
//    | SetPage (Page.Home _), _
//        when (model.State.Configuration |> function ConfigurationState.NotInitialized -> true | _ -> false) ->
//            {model with BufferedCommand = Cmd.ofMsg(message)}, Cmd.none
////               model, Cmd.none

    | SetPage (Page.Home _), _ ->
        initHome jsRuntime model
    
    | SetPage (Start), _
    | _ -> model, Cmd.none

let defaultPageModel (jsRuntime: IJSRuntime) =
    function
    | Login model -> Router.definePageModel model (Login.init jsRuntime |> fst)
    | Home (model) ->
        Router.definePageModel model (Home.init jsRuntime ConfigurationState.NotInitialized |> fst)
    | Start -> ()

let buildRouter (jsRuntime: IJSRuntime) =
    Router.inferWithModel SetPage (fun m -> m.Page) (defaultPageModel jsRuntime)

type NavbarTemplate = Template<"wwwroot/navbar.html">

type NavbarEndItemsDisplay() =
    inherit ElmishComponent<Common.AuthState, Message>()

    override this.View model dispatch =
        cond model
        <| function
        | Common.AuthState.NotTried
        | Common.AuthState.Failed ->
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
        | Common.AuthState.Success { User = user } ->
            concat
                [ div [attr.``class`` <| String.concat " " [ Bulma.``navbar-item``; Bulma.``has-dropdown``; Bulma.``is-hoverable`` ]] [
                    a [ attr.``class`` Bulma.``navbar-link`` ] [
                        text user
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
        ecomp<NavbarEndItemsDisplay, _, _> [] model.State.Authentication dispatch

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
        let remote = this.Remote<AuthService>()
        let router = buildRouter (this.JSRuntime)
        let update = update (this.JSRuntime) remote
        Program.mkProgram (fun _ -> init) (update) (view this.JSRuntime)
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
