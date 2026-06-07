# Z80 hot path design

## Problem projektowy

Obecny emulator jest zaprojektowany pod czytelność pojedynczych instrukcji, ale ZEXDOC/ZEXALL wymagają szybkiego interpretera. Główny błąd to generyczny hot path: jedna prosta instrukcja przechodzi przez dispatch, helper wyboru rejestru, helper ALU i wiele wywołań ustawiania flag.

## Błędny wzorzec

Przykład podobny do obecnego stylu:

```csharp
public static void Execute(Cpu cpu, byte opcode)
{
    int op = (opcode >> 3) & 7;
    int src = opcode & 7;
    byte value = RegisterHelper.GetRegister(cpu, src);
    byte a = cpu.Regs.A;

    cpu.Regs.A = op switch
    {
        0 => AluHelper.AddA(cpu, a, value, false),
        1 => AluHelper.AddA(cpu, a, value, true),
        2 => AluHelper.SubA(cpu, a, value, false),
        _ => a
    };

    cpu.Cycles += src == 6 ? (byte)7 : (byte)4;
}

public static byte AddA(Cpu cpu, byte a, byte b, bool withCarry)
{
    int carry = withCarry && cpu.Regs.IsCarry ? 1 : 0;
    int result = a + b + carry;

    cpu.Regs.SetFlag(CpuFlags.Zero, (result & 0xFF) == 0);
    cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
    cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFF);
    cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((a & 0x0F) + (b & 0x0F) + carry) > 0x0F);
    cpu.Regs.SetFlag(CpuFlags.Subtract, false);
    cpu.Regs.SetFlag(CpuFlags.ParityOverflow, (~(a ^ b) & (a ^ result) & 0x80) != 0);

    return (byte)result;
}
```

Dlaczego to jest wolne:

- `RegisterHelper.GetRegister` robi switch dla każdej instrukcji.
- `AluHelper` rozbija prostą operację na kolejne wywołanie.
- `SetFlag` jest wołane kilka razy.
- Każda flaga modyfikuje `F` osobno.
- `Cpu.Regs` jako property pojawia się w hot path wiele razy.

## Poprawny wzorzec

Szybki interpreter powinien mieć wyspecjalizowane handlery dla najczęstszych opcode albo generowany kod per opcode. Prosta instrukcja powinna działać lokalnie na rejestrach i przypisywać `F` raz.

```csharp
public static void AddAB(Cpu cpu, byte opcode)
{
    Registers r = cpu.Regs;
    byte a = r.A;
    byte b = r.B;
    int result = a + b;
    byte value = (byte)result;

    r.A = value;
    r.F = (byte)(
        (value & 0x80) |
        (value == 0 ? 0x40 : 0) |
        (((a ^ b ^ result) & 0x10) != 0 ? 0x10 : 0) |
        (value & 0x28) |
        ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
        (result > 0xFF ? 0x01 : 0));

    cpu.Cycles += 4;
}
```

Dla operacji logicznych używać tablicy flag:

```csharp
private static readonly byte[] SzpFlags = BuildSzpFlags();

public static void AndAB(Cpu cpu, byte opcode)
{
    Registers r = cpu.Regs;
    byte value = (byte)(r.A & r.B);

    r.A = value;
    r.F = (byte)(SzpFlags[value] | 0x10 | (value & 0x28));
    cpu.Cycles += 4;
}

private static byte[] BuildSzpFlags()
{
    byte[] flags = new byte[256];
    for (int i = 0; i < flags.Length; i++)
    {
        byte value = (byte)i;
        int bits = value;
        bits ^= bits >> 4;
        bits ^= bits >> 2;
        bits ^= bits >> 1;

        flags[i] = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (((bits & 1) == 0) ? 0x04 : 0));
    }

    return flags;
}
```

## Docelowa architektura

- `InstructionHandler[256]` nadal zostaje, ale wpisy dla hot opcode wskazują na specjalizowane handlery.
- ALU register-register ma osobne metody albo wygenerowany kod dla `ADD A,B`, `ADD A,C`, `ADD A,D` itd.
- `RegisterHelper` zostaje tylko dla zimnych ścieżek albo testowych narzędzi.
- Flagi są liczone lokalnie i przypisywane do `F` raz.
- Parity/S/Z dla 8-bit wyników idzie z tablicy.
- ROM-y zgodnościowe są regresją logiczną; wydajność mierzona osobnym benchmarkiem.

## Kolejność migracji

1. Dodać `SzpFlags[256]`.
2. Przepisać `AluHelper` tak, żeby zwracał wynik i flagi bez wielokrotnego `SetFlag`.
3. Wyspecjalizować `ALU A,r` dla rejestrów `B,C,D,E,H,L,A`.
4. Dopiero potem ruszać `Cpu.Step` i dispatch.
