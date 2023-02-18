using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PacketResolve : MonoBehaviour
{
    private const int _HEADER_LEN = 6;
    private const int _MAX_BUFFER_SIZE = 65535;
    private byte[] _tempBuffer;
    private int _packetSizeLeft;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            this._SimulateStandardPacketResolve();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            this._SimulateSplitPacketResolve();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            this._SimulateStickPacketResolve();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            this._SimulateSplitAndStickPacketResolve();
        }
    }

    private void _SimulateStandardPacketResolve()
    {
        Debug.Log("���`�ʥ]����");

        // �s�@����ʥ]�A�@38 Byte
        var sendBuffer = this._Generate19BytePacket();
        sendBuffer.AddRange(this._Generate19BytePacket());

        this._ResolvePacket(sendBuffer.ToArray());
    }

    private async Task _SimulateSplitPacketResolve()
    {
        Debug.Log("���]����");

        var sendBuffer = this._Generate19BytePacket().ToArray();

        // �s�@���]�A�e5 Byte
        byte[] splitBuffer = new byte[5];
        Buffer.BlockCopy(sendBuffer, 0, splitBuffer, 0, 5);

        // �ѥ]
        this._ResolvePacket(splitBuffer);

        Debug.Log($"�i���]�����j����5���Server�o�e�Ѿl�ʥ]��Client");
        await Task.Delay(5000);

        // �s�@���]�A��14 Byte
        splitBuffer = new byte[14];
        Buffer.BlockCopy(sendBuffer, 5, splitBuffer, 0, 14);

        // �ѥ]
        this._ResolvePacket(splitBuffer);
    }

    private async Task _SimulateStickPacketResolve()
    {
        Debug.Log("�H�]����");

        var sendBuffer = this._Generate19BytePacket();

        // �s�@�H�]�A�e6 Byte
        byte[] stickBuffer = new byte[6];
        Buffer.BlockCopy(sendBuffer.ToArray(), 0, stickBuffer, 0, 5);

        // �[�W�H�]�A�����ʥ]�@25 Byte
        sendBuffer.AddRange(stickBuffer);

        // �ѥ]
        this._ResolvePacket(sendBuffer.ToArray());

        Debug.Log($"�i�H�]�����j����5���Server�o�e�Ѿl�ʥ]��Client");
        await Task.Delay(5000);

        // �s�@�H�]�A��13 Byte
        stickBuffer = new byte[13];
        Buffer.BlockCopy(sendBuffer.ToArray(), 6, stickBuffer, 0, 13);

        // �ѥ]
        this._ResolvePacket(stickBuffer);
    }

    private async Task _SimulateSplitAndStickPacketResolve()
    {
        Debug.Log("�� + �H�]����");

        var sendBuffer = this._Generate19BytePacket();

        // �s�@�H�]�A�e6 Byte
        byte[] stickBuffer = new byte[6];
        Buffer.BlockCopy(sendBuffer.ToArray(), 0, stickBuffer, 0, 6);

        // �[�W�H�]�A�����ʥ]�@25 Byte
        sendBuffer.AddRange(stickBuffer);

        // �ѥ]
        this._ResolvePacket(sendBuffer.ToArray());

        Debug.Log($"�i�� + �H�]�����j����5���Server�o�e3 Byte���]��Client");
        await Task.Delay(5000);

        // �s�@���]�A7~9 Byte�A�@3 Byte
        var splitBuffer = new byte[3];
        Buffer.BlockCopy(sendBuffer.ToArray(), 6, splitBuffer, 0, 3);

        // �ѥ]
        this._ResolvePacket(splitBuffer.ToArray());

        Debug.Log($"�i�� + �H�]�����j����5���Server�o�e�Ѿl���]��Client");
        await Task.Delay(5000);

        // �s�@���]�A10~19 Byte�A�@10 Byte
        splitBuffer = new byte[10];
        Buffer.BlockCopy(sendBuffer.ToArray(), 9, splitBuffer, 0, 10);

        // �ѥ]
        this._ResolvePacket(splitBuffer.ToArray());
    }

    private List<byte> _Generate19BytePacket()
    {
        List<byte> datas = new List<byte>();

        // 1 Byte
        BufferTool.WriteByte(20, datas);
        // 8 Byte
        BufferTool.WriteLong(99, datas);
        // 4 Byte
        BufferTool.WriteInt(5, datas);

        // Header = 6 Byte
        var buffer = BufferTool.WriteHeader(datas, 10, 3);

        return buffer;
    }

    private void _ResolvePacket(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0) return;

        if (this._tempBuffer == null)
        {
            this._packetSizeLeft = 0;
            this._tempBuffer = new byte[PacketResolve._MAX_BUFFER_SIZE];
        }

        // �ƻs��w�s�ʥ]��
        Buffer.BlockCopy(buffer, 0, this._tempBuffer, this._packetSizeLeft, buffer.Length);

        Debug.Log($"�ƻs�����ʥ]�ܫʥ]�w�s...��m���_�l��m : {this._packetSizeLeft}, ��m��Byte�ƶq : {buffer.Length}");

        this._packetSizeLeft += buffer.Length;

        Debug.Log($"��e�Ѿl�ʥ]�ƶq : {this._packetSizeLeft}");

        int bufferReadPos = 0;
        int resolveNum = 0;

        while (this._packetSizeLeft > 0)
        {
            // �p��Ū�����ƶq
            int bufferReadedSize = this._ReadPacket(this._tempBuffer, this._packetSizeLeft, bufferReadPos);

            if (bufferReadedSize > 0)
            {
                // �W�[�wŪ�����ʥ]Size��Pos
                bufferReadPos += bufferReadedSize;

                // �NBuffer Offset�������wŪ�����ʥ]�ƶq
                this._packetSizeLeft -= bufferReadedSize;

                Debug.Log($"Ū���쪺Packet�ƶq : {bufferReadedSize}, �Ѿl���ʥ]�ƶq : {this._packetSizeLeft}, Ū��Pos : {bufferReadPos}");

                resolveNum++;

                if (this._packetSizeLeft == 0)
                {
                    Debug.Log($"<color=#1ACB00>�ʥ]�w����ѪR�A����ѪR</color>");
                    break;
                }
                else
                {
                    Debug.Log($"<color=#FF8400>�ʥ]������ѪR�A�Ѿl�nŪ�����ƶq : {this._packetSizeLeft}�A�N�i��U�@���j��@�ѥ]</color>");
                }
            }
            else if (bufferReadedSize == 0)
            {
                // �p�GBufferReadPos > 0�A�h�N��Ѿl���ʥ]�ä��bBuffer�̫e�ݡA�N�䲾�ʨ�̫e�ݡA�H�Q�U�@���ʥ]Ū���B�z
                if (bufferReadPos > 0)
                {
                    // �P�_�w���]�A�N�Ѿl��Buffer���Ʋ��̫ܳe�ݡA�åBbreak���j��A���ݤU�@�ӫʥ]�����...
                    BufferTool.MoveBufferToFront(this._tempBuffer, bufferReadPos, this._packetSizeLeft);

                    Debug.Log($"<color=#FF8400>�P�_�w���]...�BReadPos > 0�A�N����Buffer����Ʀ̫ܳe�� : ���ʰ_�lPos : {bufferReadPos}, ����Byte�ƶq : {this._packetSizeLeft}</color>");
                }
                else
                {
                    Debug.Log($"<color=#FF8400>�P�_�w���]�AReadPos = 0�A�L�ݲ���Buffer�ܫe��</color>");
                }

                Debug.Log($"�N���ݤU�@���ʥ]�����...");
                break;
            }
        }

        Debug.Log($"<color=#1ACB00>�ʥ]����ѪR���ƶq : {resolveNum}</color>");
    }

    private int _ReadPacket(byte[] buffer, int packetSizeLeft, int readPos)
    {
        // �������Y���סA�����H�ѪR
        if (packetSizeLeft < PacketResolve._HEADER_LEN)
        {
            Debug.Log($"<color=#FF8400>�ʥ]���פ����w�q���Y���� => Actual Length : {packetSizeLeft}, less than required Header Length : {PacketResolve._HEADER_LEN}, needs more packet</color>");
            return 0;
        }

        int pos = readPos;
        Debug.Log($"��eŪ��Pos : {pos}");

        // ���X�Y2 Byte�A�o�X���׸�T
        short length = BufferTool.GetShort(buffer, ref pos);

        // ��ڱ����쪺�ʥ]��T < �ѪR�X�����׸�T = ���]
        if (packetSizeLeft < length)
        {
            Debug.Log($"<color=#FF8400>���] => Expected Length : {length}, Actual Length : {packetSizeLeft}, needs more packet</color>");
            return 0;
        }
        // �ʥ]�����A����ѪR��2�ر��p
        // 1. PacketLeftSize = Length >>> ����ѥ]
        // 2. PacketLeftSize > Length >>> �H�]
        else
        {
            Debug.Log($"<color=#1ACB00>�ʥ]�����A�i��g�J��Client�ѥ]���O</color>");
            this._WriteIntoNetProtocolResolver(buffer);
        }

        return length;
    }

    private void _WriteIntoNetProtocolResolver(byte[] buffer)
    {
        // �����y�{���O�z�L�ѥX��FuncId���oClient�������ѥ]���O�ð��ѥ]

        // ���B�u�O²������ʥ]��ƪ����X
        int pos = 0;
        Debug.Log($"<color=#00C4D1>�ѥ] Length : {BufferTool.GetShort(buffer, ref pos)}</color>");
        Debug.Log($"<color=#00C4D1>�ѥ] FuncId : {BufferTool.GetShort(buffer, ref pos)}</color>");
        Debug.Log($"<color=#00C4D1>�ѥ] ProtocolType : {BufferTool.GetShort(buffer, ref pos)}</color>");

        byte d1 = BufferTool.GetByte(buffer, ref pos);
        long d2 = BufferTool.GetLong(buffer, ref pos);
        short d3 = BufferTool.GetShort(buffer, ref pos);

        Debug.Log($"<color=#00C4D1>�ʥ]��ƸѪR D1 : {d1}, D2 : {d2}, D3 : {d3}</color>");
    }
}
