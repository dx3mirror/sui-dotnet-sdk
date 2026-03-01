namespace MystenLabs.Sui.Utils;

/// <summary>
/// Encodes and decodes byte sequences as hexadecimal strings (lowercase, optional 0x prefix).
/// </summary>
public static partial class Hex
{
    private const string HexPrefix = "0x";
    private const int HighNibbleShift = 4;
    private const int LowNibbleMask = 0x0F;
    private const int NibblesPerByte = 2;
    private const string HexDigits = "0123456789abcdef";

    private const char DigitZero = '0';
    private const char DigitNine = '9';
    private const char LetterLowerA = 'a';
    private const char LetterLowerF = 'f';
    private const char LetterUpperA = 'A';
    private const char LetterUpperF = 'F';
    private const int DecimalDigitCount = 10;

    /// <summary>
    /// Decodes a hexadecimal string into a byte array. Accepts optional "0x" prefix.
    /// Odd-length strings are treated as if padded with a leading zero (compatible with TS fromHex).
    /// </summary>
    /// <param name="hex">Hexadecimal string (e.g. "0x1a2b", "1a2b", or "a2b").</param>
    /// <returns>Decoded bytes. Length is half of the hex character count (after removing prefix), rounded up for odd length.</returns>
    /// <exception cref="ArgumentException">Thrown when a character is not a valid hex digit.</exception>
    public static byte[] Decode(ReadOnlySpan<char> hex)
    {
        if (hex.StartsWith(HexPrefix))
        {
            hex = hex[HexPrefix.Length..];
        }

        if (hex.Length == 0)
        {
            return [];
        }

        bool oddLength = (hex.Length % NibblesPerByte) != 0;
        int byteCount = (hex.Length + 1) / NibblesPerByte;
        byte[] bytes = new byte[byteCount];

        for (int index = 0; index < byteCount; index++)
        {
            int high;
            int low;
            if (oddLength)
            {
                if (index == 0)
                {
                    high = 0;
                    low = Nibble(hex[0]);
                }
                else
                {
                    high = Nibble(hex[(index * NibblesPerByte) - 1]);
                    low = Nibble(hex[index * NibblesPerByte]);
                }
            }
            else
            {
                high = Nibble(hex[index * NibblesPerByte]);
                low = Nibble(hex[(index * NibblesPerByte) + 1]);
            }

            bytes[index] = (byte)((high << HighNibbleShift) | low);
        }

        return bytes;
    }

    /// <summary>
    /// Encodes bytes as a lowercase hexadecimal string without any prefix.
    /// </summary>
    /// <param name="bytes">Bytes to encode.</param>
    /// <returns>Lowercase hex string (e.g. "1a2b3c").</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        int length = bytes.Length * NibblesPerByte;
        char[] characters = new char[length];

        for (int index = 0; index < bytes.Length; index++)
        {
            byte value = bytes[index];
            characters[index * NibblesPerByte] = HexDigits[value >> HighNibbleShift];
            characters[(index * NibblesPerByte) + 1] = HexDigits[value & LowNibbleMask];
        }

        return new string(characters);
    }

    /// <summary>
    /// Returns true if the character is a valid hex digit (0–9, a–f, A–F).
    /// </summary>
    public static bool IsHexChar(char character)
    {
        return (character >= DigitZero && character <= DigitNine)
            || (character >= LetterLowerA && character <= LetterLowerF)
            || (character >= LetterUpperA && character <= LetterUpperF);
    }

    /// <summary>
    /// Converts a single hex digit character to its numeric value (0–15).
    /// </summary>
    /// <param name="character">A hex digit: 0–9, a–f, or A–F.</param>
    /// <returns>Numeric value 0–15.</returns>
    /// <exception cref="ArgumentException">Thrown when the character is not a valid hex digit.</exception>
    private static int Nibble(char character)
    {
        if (character >= DigitZero && character <= DigitNine)
        {
            return character - DigitZero;
        }

        if (character >= LetterLowerA && character <= LetterLowerF)
        {
            return character - LetterLowerA + DecimalDigitCount;
        }

        if (character >= LetterUpperA && character <= LetterUpperF)
        {
            return character - LetterUpperA + DecimalDigitCount;
        }

        throw new ArgumentException($"Invalid hex character: {character}", nameof(character));
    }
}
