# Comments

Comments are parts of the code that are ignored during compilation and execution.
Comments can be added using `//`. Anything on the same line after the `//` is a comment and will be ignored.

```gleam
// this is a comment
let x = 1 + 2 // trailing comments are fine too
```

This can be useful to add notes to the code, and also to temporarily disable parts of the code without deleting them.

A `///` comment before a `pub fn` is a **doc comment**: it becomes the popup information that appears when you hover the function name in your editor.

```gleam
/// This function does nothing.
pub fn f() {
  Nil
}
```