using System;
using UnityEngine;

public static class ColorUtils
{
    public static byte[] ColorToByte(ref Color c)
    {
        return new byte[4]
        {
            (byte)Mathf.FloorToInt(c.r * 255.0f),
            (byte)Mathf.FloorToInt(c.g * 255.0f),
            (byte)Mathf.FloorToInt(c.b * 255.0f),
            (byte)Mathf.FloorToInt(c.a * 255.0f)
        };
    }
    public static void ByteToColor(ArraySegment<byte> b, ref Color c)
    {
        c.r = b[0] / 255.0f;
        c.g = b[1] / 255.0f;
        c.b = b[2] / 255.0f;
        c.a = b[3] / 255.0f;
    }
    public static byte Scale8(byte i, byte scale)
    {
        return (byte)(((int)i * (1 + (int)scale)) >> 8);
    }
}
