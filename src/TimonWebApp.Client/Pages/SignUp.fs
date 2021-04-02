module TimonWebApp.Client.Pages.SignUp

open System
open System.Text.RegularExpressions
open Elmish
open TimonWebApp.Client.BoleroHelpers
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open TimonWebApp.Client.Pages.Controls.InputsHtml
open Bolero.Html
open Bolero
open TimonWebApp.Client.ClubServices
open TimonWebApp.Client.AuthServices
open TimonWebApp.Client.LinkServices
open TimonWebApp.Client.ChannelServices
open TimonWebApp.Client.Dtos

type Model =
    { failureReason: string option
      form: SignUpRequest
      validatedForm: Result<SignUpRequest, Map<string, string list>> option
      focus: string option
      isLoading: bool }
    static member Default =
        { failureReason = None
          form =
              { userName = ""
                firstName = ""
                lastName = ""
                email = ""
                password = ""
                confirmPassword = "" }
          validatedForm = None
          focus = None
          isLoading = false }

let init _ = Model.Default, Cmd.none

type Message =
    | ValidateForm
    | SignUpValidated
    | SignUpSuccess of string option
    | SignUpError of exn
    | Focused of string
    | SetFormField of string * string
    | FormValidated

let validateForm (form: SignUpRequest) =
    let validInput (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(
            name
            + " cannot be blank"
        )
        |> validator.MinLen
            2
            (name
             + " must have more than 2 characters")
        |> validator.MaxLen
            50
            (name
             + " must have less than 50 characters")
        |> validator.End

    let validEmail (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(
            name
            + " cannot be blank"
        )
        |> validator.IsMail(
            name
            + " should be an email format"
        )
        |> validator.End

    let validPassword (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(
            name
            + " cannot be blank"
        )
        |> validator.MinLen
            6
            (name
             + " must have more than 6 characters")
        |> validator.MaxLen
            50
            (name
             + " must have less than 100 characters")
        |> validator.Match(Regex("[A-Z]")) ("passwords must have at least one uppercase")
        |> validator.Match(Regex("[0-9]")) ("passwords must have at least one digit")
        |> validator.Match(Regex("[^a-zA-Z\d\s:]")) ("passwords must have at least one non alphanumeric character")
        |> validator.Match(Regex(form.confirmPassword)) ("passwords must match")
        |> validator.End

    all
    <| fun t ->
        { userName = validInput t "Username" form.userName
          firstName = validInput t "Firstname" form.firstName
          lastName = validInput t "Lastname" form.lastName
          email = validEmail t "Email" form.email
          password = validPassword t "Password" form.password
          confirmPassword = validPassword t "Confirm password" form.confirmPassword }

let update (timonService: TimonService) message (model: Model) =
    let validateForced form =
        let validated = validateForm form

        { model with
              form = form
              validatedForm = Some validated
              failureReason = None }

    let validate form =
        match model.validatedForm with
        | None ->
            { model with
                  form = form
                  failureReason = None }
        | Some _ -> validateForced form

    match message, model with
    | SetFormField ("Firstname", value), _ ->
        { model.form with firstName = value }
        |> validate,
        Cmd.none
    | SetFormField ("Lastname", value), _ ->
        { model.form with lastName = value }
        |> validate,
        Cmd.none
    | SetFormField ("Username", value), _ ->
        { model.form with userName = value }
        |> validate,
        Cmd.none
    | SetFormField ("Email", value), _ ->
        { model.form with email = value }
        |> validate,
        Cmd.none
    | SetFormField ("Password", value), _ ->
        { model.form with password = value }
        |> validate,
        Cmd.none
    | SetFormField ("Confirm password", value), _ ->
        { model.form with
              confirmPassword = value }
        |> validate,
        Cmd.none

    | ValidateForm, _ ->
        model.form
        |> validateForced,
        Cmd.ofMsg FormValidated

    | FormValidated, _ ->
        let cmd =
            Cmd.OfAsync.either signUp (timonService, model.form) SignUpSuccess SignUpError

        { model with isLoading = true }, cmd

    | SignUpSuccess _, _ ->
        let form =
            { model.form with
                  firstName = String.Empty
                  lastName = String.Empty
                  userName = String.Empty
                  email = String.Empty
                  password = String.Empty
                  confirmPassword = String.Empty }

        { model with
              isLoading = false
              form = form },
        Cmd.none

    | SignUpError exn, _ ->
        { model with
              failureReason = Some exn.Message
              isLoading = false },
        Cmd.none

    | _, _ -> model, Cmd.none

type SignUpPage = Template<"wwwroot/signUp.html">

let view model dispatch =
    let errorHole =
        match model.failureReason with
        | Some (value) ->
            div [ attr.``class``
                  <| String.concat " " [ Bulma.``is-danger``; Bulma.message ] ] [
                div [ attr.``class`` Bulma.``message-header`` ] [
                    text value
                ]
            ]
        | None -> empty

    let formFieldItem =
        formFieldItem model.validatedForm model.focus model.isLoading

    let pd name =
        fun v -> dispatch (SetFormField(name, v))

    let signUpForm =
        div [] [
            concat [
                comp<KeySubscriber> [] []
                formFieldItem "text" "Firstname" model.form.firstName (pd "Firstname")
                formFieldItem "text" "Lastname" model.form.lastName (pd "Lastname")
                formFieldItem "text" "Username" model.form.userName (pd "Username")
                formFieldItem "email" "Email" model.form.email (pd "Email")
                formFieldItem "password" "Password" model.form.password (pd "Password")
                formFieldItem "password" "Confirm password" model.form.confirmPassword (pd "Confirm password")
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
                         on.click (fun _ -> dispatch ValidateForm) ] [
                    text "Sign up"
                ]
            ]
        ]

    SignUpPage()
        .errorHole(errorHole)
        .singUpForm(signUpForm)
        .Elt()
