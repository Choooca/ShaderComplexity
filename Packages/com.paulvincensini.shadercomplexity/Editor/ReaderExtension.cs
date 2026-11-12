using UnityEngine;

public static class ReaderExtension
{
    public static uint ToFourCC(this string code)
    {
        if (code.Length != 4)
            throw new System.Exception($"You try to translate a code with {code.Length} characters to four characters");

        var a = (byte)code[0];
        var b = (byte)code[1];
        var c = (byte)code[2];
        var d = (byte)code[3];

        return a | ((uint)(b << 8)) | ((uint)(c << 16)) | ((uint)(d << 24));
    }

    public static string FourCCToString(this uint code)
    {
        var a = (char)(code & 0xFF);
        var b = (char)((code >> 8) & 0xFF);
        var c = (char)((code >> 16) & 0xFF);
        var d = (char)((code >> 24) & 0xFF);

        return new string(new[] { a, b, c, d });
    }
}
