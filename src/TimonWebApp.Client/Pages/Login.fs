module TimonWebApp.Client.Pages.Login

open System
open Elmish
open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client.BoleroHelpers
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Pages.Controls.InputsHtml
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation

type Model =
    { failureReason: string option
      isSignedIn: bool
      currentLogin: LoginRequest
      validatedLogin: Result<LoginRequest, Map<string, string list>> option
      isLoading: bool
      focus: string option }
    static member Default =
        { currentLogin = { email = ""; password = "" }
          validatedLogin = None
          isSignedIn = false
          failureReason = None
          focus = None
          isLoading = false }

type Message =
    | DoLogin
    | LoginValidated
    | LoginSuccess of option<string>
    | SignInSuccessful of string
    | LoginError of exn
    | Focused of string
    | SetFormField of string * string

let init _ = Model.Default, Cmd.none

let validateForm (form: LoginRequest) =
    let cannotBeBlank (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank
            (name
             + " cannot be blank")
        |> validator.End

    let validEmail (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank
            (name
             + " cannot be blank")
        |> validator.IsMail
            (name
             + " should be an email format")
        |> validator.End

    all
    <| fun t ->
        { email = validEmail t "Email" form.email
          password = cannotBeBlank t "Password" form.password }

let update (timonService: TimonService) message model =
    let validateForced form =
        let validated = validateForm form
        { model with
              currentLogin = form
              validatedLogin = Some validated
              failureReason = None }

    let validate form =
        match model.validatedLogin with
        | None ->
            { model with
                  currentLogin = form
                  failureReason = None }
        | Some _ -> validateForced form

    match message, model with
    | Focused field, _ -> { model with focus = Some field }, Cmd.none

    | SetFormField ("Email", value), _ ->
        { model.currentLogin with
              email = value }
        |> validate,
        Cmd.none

    | SetFormField ("Password", value), _ ->
        { model.currentLogin with
              password = value }
        |> validate,
        Cmd.none

    | _, ({ validatedLogin = Some (Error _) }) -> model, Cmd.none

    | DoLogin, _ ->
        model.currentLogin
        |> validateForced,
        Cmd.ofMsg (LoginValidated)

    | LoginValidated, _ ->
        let loginRequest =
            { email = model.currentLogin.email
              password = model.currentLogin.password }


        { model with isLoading = true },
        Cmd.OfAsync.either
            logIn
            (timonService, loginRequest)
            LoginSuccess
            LoginError

    | LoginSuccess _, _ ->
        let currentLoginForm =
            { model.currentLogin with
                  email = String.Empty
                  password = String.Empty }

        { model with
              isLoading = false
              currentLogin = currentLoginForm },
        Cmd.none

    //    | LoginSuccess value, _ ->
//        match value with
//        | Some loginResponse -> { model with IsSignedIn = true}, Cmd.ofMsg(SignInSuccessful loginResponse)
//        | None -> { model with IsSignedIn = false }, Cmd.none

    | LoginError _, _ ->
        { model with
              failureReason = Some "Verify your email or password"
              isLoading = false },
        Cmd.none

    | _ -> failwith ""

let view (jsRuntime: IJSRuntime) model dispatch =
    div [ attr.``class``
          <| String.concat
              " "
                 [ Bulma.hero
                   Bulma.``is-fullheight``
                   Bulma.``is-light`` ] ] [
        div [ attr.``class`` Bulma.``hero-body`` ] [
            div [ attr.``class``
                  <| String.concat
                      " "
                         [ Bulma.container
                           Bulma.``has-text-centered`` ] ] [
                div [ attr.``class``
                      <| String.concat
                          " "
                             [ Bulma.column
                               Bulma.``is-8``
                               Bulma.``is-offset-2`` ] ] [
                    h1 [ attr.``class``
                         <| String.concat
                             " "
                                [ Bulma.title; Bulma.``has-text-grey`` ] ] [
                        text "Log in"
                    ]
                    hr [ attr.``class`` "login-hr" ]
                    div [ attr.``class`` Bulma.box ] [
                        div [ attr.``class`` Bulma.box ] [
                            img [ attr.src "images/timon_logo.png" ]
                        ]
                        div [ attr.``class``
                              <| String.concat
                                  " "
                                     [ Bulma.title
                                       Bulma.``has-text-grey``
                                       Bulma.``is-5`` ] ] [
                            text "Please enter your email and password"
                        ]
                        match model.failureReason with
                        | Some (value) ->
                            div [ attr.``class``
                                  <| String.concat
                                      " "
                                         [ Bulma.``is-danger``; Bulma.message ] ] [
                                div [ attr.``class`` Bulma.``message-header`` ] [
                                    text value
                                ]
                            ]
                        | None -> ()

                        let formFieldItem =
                            formFieldItem
                                model.validatedLogin
                                model.focus
                                model.isLoading

                        let pd name =
                            fun v -> dispatch (SetFormField(name, v))

                        div [] [
                            concat [
                                comp<KeySubscriber> [] []
                                formFieldItem
                                    "email"
                                    "Email"
                                    model.currentLogin.email
                                    (pd "Email")
                                formFieldItem
                                    "password"
                                    "Password"
                                    model.currentLogin.password
                                    (pd "Password")
                                button [ attr.id "confirmButton"
                                         attr.``class``
                                         <| String.concat
                                             " "
                                                [ Bulma.button
                                                  Bulma.``is-block``
                                                  Bulma.``is-primary``
                                                  Bulma.``is-large``
                                                  Bulma.``is-fullwidth``
                                                  if model.isLoading then
                                                      Bulma.``is-loading``
                                                  else
                                                      "" ]
                                         attr.disabled model.isLoading
                                         on.click (fun _ -> dispatch DoLogin) ] [
                                    text "Log in"
                                ]
                            ]
                        ]
                    ]
                    div [ attr.``class`` Bulma.columns ] [
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href "/sign-up"
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Sign up"
                            ]
                        ]
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href ""
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Forgot password"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
