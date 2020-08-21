module TimonWebApp.Client.Pages.Controls

open Bolero.Html
open TimonWebApp.Client.Common

let errorAndClass name onFocus (result:Result<_,Map<_,_>> option) =
      match result, onFocus with
      | _ , Some focus when focus = name -> None,""
      | Some (Error e), _ when (e.ContainsKey name && e.[name] <> []) -> Some(System.String.Join(",", e.[name])), Bulma.``is-danger``
      | Some _, _ -> None, "modified valid"
      | _ -> None,""
      
let formFieldItem  item onFocus focusMessage fieldType name value callback =
    let error, validClass = errorAndClass name onFocus item
    div [ attr.``class`` "field" ] [
        div [ attr.``class`` "control" ] [
            input [ attr.``class`` <| String.concat " " [ Bulma.input; Bulma.``is-large``; validClass ]
                    attr.``type`` fieldType
                    attr.placeholder name
                    attr.autofocus ""
                    bind.input.string value callback
                    ]
            match error with
            | Some value ->
                span [attr.``class`` <| String.concat " " [ Bulma.``has-text-danger`` ]]
                    [text value]
            | None -> ()
        ]
    ]