# Operators

arithmetic operators: `+`, `-`, `*`, `/`, `%`
comparison operators: `==`, `!=`, `<=`, `>=`, `<`, `>`
boolean operators: `!`, `&&`, `||`

Note: The game's numbers are floating point numbers. Use `gleam/float` for float math and `gleam/int` for integer math. `int.` operators on floats need explicit conversion.

## Introduction

Operators allow you to compare, modify and combine values.
The arithmetic operators `+`, `-`, `*`, `/`, `%` are used to perform common mathematical operations on numbers.
The comparison operators `==`, `!=`, `<=`, `>=`, `<`, `>` are used to compare values. The result is always either `True` or `False`.
The logic operators (also called boolean operators) `!`, `&&`, `||` are used to combine truth values.

## Arithmetic Operators

`+` and `-` are used for addition and subtraction.

`2 + 3` evaluates to `5`
`3 - 2` evaluates to `1`

`*` and `/` are used for multiplication and division.

`2 * 3` evaluates to `6`
`5 / 2` evaluates to `2.5`

`%` is the modulo operator, also known as the remainder operator. It essentially divides the two numbers and then returns the remainder. You can also think of it as repeatedly subtracting the right number from the left number until the remainder is less than the right number.

`4 % 2` evaluates to `0`
`5 % 2` evaluates to `1`
`6 % 2` evaluates to `0`
`2 % 6` evaluates to `2`

Use `int.power(base, of: exponent)` for powers.

`int.power(2, of: 2.0)` evaluates to `4.0`

## Comparison Operators

`==` and `!=` are used to check if two values are "equal"(`==`) or "not equal"(`!=`). They can be used on all types of values.

`2 == 2` evaluates to `True`
`game.Bush != game.Bush` evaluates to `False`
`3 != 3 + 1` evaluates to `True`

`<=, >=, <, >` can only be used on numbers. They check if the left number is "smaller or equal"(`<=`), "bigger or equal"(`>=`), "smaller" (`<`) or "bigger" (`>`) than the right number.

`1 <= 1` evaluates to `True`
`2 >= 3` evaluates to `False`
`-2 < -1` evaluates to `True`
`6 > 6` evaluates to `False`

## Logic Operators

`!` simply inverts the value:

`!False` evaluates to `True`
`!True` evaluates to `False`

`&&` evaluates to `True` only if both values are `True`

`True && True` evaluates to `True`
`True && False` evaluates to `False`
`False && False` evaluates to `False`

`||` evaluates to `True` if at least one of the values is `True`

`True || True` evaluates to `True`
`True || False` evaluates to `True`
`False || False` evaluates to `False`