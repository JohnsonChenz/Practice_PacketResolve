using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BufferTool
{
    public static byte GetByte(byte[] buffer, ref int pos)
    {
        byte value = buffer[pos];

        pos += 1;

        return value;
    }

    public static short GetShort(byte[] buffer, ref int pos)
    {
        byte[] data = new byte[2];

        Buffer.BlockCopy(buffer, pos, data, 0, 2);

        pos += 2;

        short value = BitConverter.ToInt16(data);

        return value;
    }

    public static int GetInt(byte[] buffer, ref int pos)
    {
        byte[] data = new byte[4];

        Buffer.BlockCopy(buffer, pos, data, 0, 4);

        pos += 4;

        int value = BitConverter.ToInt32(data);

        return value;
    }

    public static long GetLong(byte[] buffer, ref int pos)
    {
        byte[] data = new byte[8];

        Buffer.BlockCopy(buffer, pos, data, 0, 8);

        pos += 8;

        long value = BitConverter.ToInt64(data);

        return value;
    }

    public static void WriteByte(byte value, List<byte> buffer)
    {
        buffer.AddRange(new byte[] { value });
    }

    public static void WriteShort(short value, List<byte> buffer)
    {
        byte[] data = BitConverter.GetBytes(value);

        buffer.AddRange(data);
    }

    public static void WriteInt(int value, List<byte> buffer)
    {
        byte[] data = BitConverter.GetBytes(value);

        buffer.AddRange(data);
    }

    public static void WriteLong(long value, List<byte> buffer)
    {
        byte[] data = BitConverter.GetBytes(value);

        buffer.AddRange(data);
    }

    public static void WriteBytes(byte[] value, List<byte> buffer)
    {
        buffer.AddRange(value);
    }

    public static List<byte> WriteHeader(List<byte> datas, short funcId, short protocolType)
    {
        // �p�����
        short length = (short)(datas.Count + 2 + 2 + 2);

        Debug.Log($"�ʥ]�`���� : {length}");

        List<byte> buffer = new List<byte>();

        // ��J����, 2 Bytes
        BufferTool.WriteShort(length, buffer);

        // ��JFuncId, 2 Bytes
        BufferTool.WriteShort(funcId, buffer);

        // ��JProtocolType, 2 Bytes
        BufferTool.WriteShort(protocolType, buffer);

        // ��J���, N Bytes
        buffer.AddRange(datas);

        return buffer;
    }

    public static void MoveBufferToFront(byte[] buffer, int startPos, int length)
    {
        Buffer.BlockCopy(buffer, startPos, buffer, 0, length);
    }
}
