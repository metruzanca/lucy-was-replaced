# Debug

Sometimes your code just doesn't work and you need to find out why. There are a couple of tools to help you do that.

The first is to execute the program step by step.
You can go into step-by-step mode with the button next to the Execute button or by setting a breakpoint.

Breakpoints can be added by clicking on the breakpoint panel to the left of the code.
![](Breakpoints227)
When execution reaches the line where the breakpoint is, it will automatically switch to step-by-step mode.

When you move your mouse over a variable, its current value is displayed.

Printing values is also very useful. The `echo` keyword writes any value to the output, just like the game's Python `print()`. It is free and unpaced.

```gleam
echo 0.24
echo game.can_harvest()
echo game.get_pos()
```

`io.println()` prints a string with a line break, paced like the game's `print()`. Build a string from any value with `string.inspect`:

```gleam
import gleam/io
import gleam/string

io.println("0.24")
io.println(string.inspect(game.can_harvest()))
```

The output window also logs warnings and errors, so if something doesn't work as expected it can be useful to check that.

When the execution stops, the output is also written to the output.txt file in the game folder. [output.txt](persistent_data_path/output.txt).