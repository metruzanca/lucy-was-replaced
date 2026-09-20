# First Program
## Text editor
All programming is done in code windows. Each code window corresponds to a file containing code.
You can rename the file by clicking on its name at the top of the window.

The code can be edited like in any text editor as long as it's not running.
You can execute the program directly by pressing the green play button in the code window.
![](PlayButton50)

You can create more code files using the "+" button in the upper right corner of the screen.
You can dock a window to another window by dragging it onto it.

You will notice that once you start typing, a simple code completion window will pop up.
Press Tab to insert the code completion.
Use the arrow keys to navigate through the completion options.

Don't worry if this is your first time programming. The language is unlocked step by step, so you won't be overwhelmed by all the things you can do.

Every program starts the same way:

```gleam
import game

pub fn main() {
  // your code here
}
```

`import game` gives you the farm — the drone, the plants, and the sensors.
Your program's `main` function runs when you press the play button; write what
you want to happen between the `{` and `}`.

Currently, there are two drone commands available.

```gleam
import game

pub fn main() {
  game.harvest()
  game.do_a_flip()
}
```

These are function calls. You can think of a function as a command that can be
executed. You execute it using the `()` parentheses, and `game.` in front
tells Gleam which module the command comes from.

Try typing these statements in the code window and pressing the execute button.

You can think of your code as a sequence of statements. You can run multiple statements in a row like this:

```gleam
game.harvest()
game.do_a_flip()
game.harvest()
```

## Unlocks
Collecting grass will give you hay. Hay can be used to unlock loops in the unlock menu. Open the unlock menu with the button in the top right corner.