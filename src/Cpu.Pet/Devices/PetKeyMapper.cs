namespace Cpu.Pet.Devices;

public static class PetKeyMapper
{
    public static bool TryMapCharacter(char ch, out int row, out int col)
    {
        row = 0;
        col = 0;
        char upper = char.ToUpperInvariant(ch);

        if (upper is >= 'A' and <= 'Z')
        {
            int index = upper - 'A';
            row = index / 8;
            col = index % 8;
            return true;
        }

        if (ch is >= '0' and <= '9')
        {
            int digit = ch - '0';
            if (digit <= 7) { row = 6; col = digit; return true; }
            row = 7; col = digit - 8; return true;
        }

        return ch switch
        {
            '@' => Set(0, 0, out row, out col),
            '[' => Set(3, 3, out row, out col),
            '\\' => Set(3, 4, out row, out col),
            ']' => Set(3, 5, out row, out col),
            ' ' => Set(4, 0, out row, out col),
            '!' => Set(4, 1, out row, out col),
            '"' => Set(4, 2, out row, out col),
            '#' => Set(4, 3, out row, out col),
            '$' => Set(4, 4, out row, out col),
            '%' => Set(4, 5, out row, out col),
            '&' => Set(4, 6, out row, out col),
            '\'' => Set(4, 7, out row, out col),
            '(' => Set(5, 0, out row, out col),
            ')' => Set(5, 1, out row, out col),
            '*' => Set(5, 2, out row, out col),
            '+' => Set(5, 3, out row, out col),
            ',' => Set(5, 4, out row, out col),
            '-' => Set(5, 5, out row, out col),
            '.' => Set(5, 6, out row, out col),
            '/' => Set(5, 7, out row, out col),
            ':' => Set(7, 2, out row, out col),
            ';' => Set(7, 3, out row, out col),
            '<' => Set(7, 4, out row, out col),
            '=' => Set(7, 5, out row, out col),
            '>' => Set(7, 6, out row, out col),
            '?' => Set(7, 7, out row, out col),
            '\r' or '\n' => Set(0, 0, out row, out col),
            _ => false
        };
    }

    private static bool Set(int r, int c, out int row, out int col)
    {
        row = r;
        col = c;
        return true;
    }
}
