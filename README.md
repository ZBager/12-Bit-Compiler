# 12-Bit Compiler

An assembler written in C# (.NET) that compiles a custom assembly language into 12-bit hexadecimal machine code for a 12-bit architecture.

## Overview

This project reads a program written in a custom assembly-like language from `Data/Program.txt`, parses each instruction, and outputs 12-bit hexadecimal machine code line-by-line to stdout. It supports registers, memory addressing, arithmetic/logic operations, and conditional jumps with labels.

## Operand Types

The assembler supports three operand types:

| Type | Format | Range |
|------|--------|-------|
| Register | `REG[0]` – `REG[15]` | 0–15 |
| Immediate | Literal integer | 0–4095 |
| Memory (register-indirect) | `RAM[REG[0]]` – `RAM[REG[15]]` | 0–15 |

## Instruction Set

### Arithmetic

| Mnemonic | Operands | Description |
|----------|----------|-------------|
| `ADD` | `dest, src` | Add |
| `SUB` | `dest, src` | Subtract |
| `RSUB` | `dest, src` | Reverse subtract |

### Logic

| Mnemonic | Operands | Description |
|----------|----------|-------------|
| `AND` | `dest, src` | Bitwise AND |
| `OR` | `dest, src` | Bitwise OR |
| `XOR` | `dest, src` | Bitwise XOR |

### Data Movement

| Mnemonic | Operands | Description |
|----------|----------|-------------|
| `MOVE` | `value, dest` | Move value to register |
| `CMOVE` | `value, dest` | Conditional move |

### Comparison

| Mnemonic | Operands | Description |
|----------|----------|-------------|
| `CMP` | `a, b` | Compare two values (sets flags) |

### Unary

| Mnemonic | Operand | Description |
|----------|---------|-------------|
| `INC` | `reg` | Increment |
| `DEC` | `reg` | Decrement |

### Flow Control

| Mnemonic | Operands | Description |
|----------|----------|-------------|
| `STOP` | — | Halt execution |
| `CSTOP` | — | Conditional halt |
| `JE` / `JEL` | `a, b, label` | Jump if equal |
| `JLE` / `JEL` | `a, b, label` | Jump if less-than or equal |
| `JGE` / `JEG` | `a, b, label` | Jump if greater-than or equal |
| `JG` | `a, b, label` | Jump if greater-than |
| `JL` | `a, b, label` | Jump if less-than |
| `JO` | `label` | Jump if overflow |

### Labels

Labels are defined at the start of a line (no leading whitespace) followed by a colon:

```asm
LOOP:
    MOVE 1, REG[0]
    ...
    JE REG[0], REG[1], LOOP
```

## Example Program

A bubble sort implementation:

```asm
    MOVE 48, REG[1]
    MOVE 63, REG[7]
OUTER:
    MOVE REG[1], REG[5]
    SUB REG[7], REG[5]
    DEC REG[5]
INNER:
    MOVE RAM[REG[1]], REG[3]
    INC REG[1]
    MOVE RAM[REG[1]], REG[4]
    JGE REG[3], REG[4], SKIP_SWAP
    MOVE REG[3], RAM[REG[2]]
    DEC REG[2]
    MOVE REG[4], RAM[REG[2]]
    INC REG[1], REG[1]
SKIP_SWAP:
    JGE REG[2], REG[5], INNER
    INC REG[1]
    JGE REG[1], REG[6], OUTER
    STOP
```

## Building & Running

```bash
# Build
dotnet build

# Run (reads Compiler-12-Bit/Data/Program.txt)
dotnet run --project Compiler-12-Bit

# Output: 12-bit hexadecimal machine code printed line-by-line
```

## Syntax Rules

- **Instructions** must be indented with whitespace (leading space or tab).
- **Labels** start at column 0 (no leading whitespace) and end with `:`.
- **Registers** use the syntax `REG[N]` where N is 0–15.
- **Memory** uses the syntax `RAM[REG[N]]` where N is 0–15.
- **Immediate values** are plain integers 0–4095.
- **Comments** are not supported in the source format.

## Architecture

- **Target**: 12-bit instruction width
- **Registers**: 16 (addressed as `REG[0]`–`REG[15]`)
- **Memory addressing**: Register-indirect via `RAM[REG[N]]`
- **Program counter**: Internal; tracks output line count
- **Flags**: Cleared before conditional jumps using `E10` / `000` instructions
