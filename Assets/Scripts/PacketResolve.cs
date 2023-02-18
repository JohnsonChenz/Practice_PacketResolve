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
        Debug.Log("正常封包模擬");

        // 製作兩份封包，共38 Byte
        var sendBuffer = this._Generate19BytePacket();
        sendBuffer.AddRange(this._Generate19BytePacket());

        this._ResolvePacket(sendBuffer.ToArray());
    }

    private async Task _SimulateSplitPacketResolve()
    {
        Debug.Log("分包模擬");

        var sendBuffer = this._Generate19BytePacket().ToArray();

        // 製作分包，前5 Byte
        byte[] splitBuffer = new byte[5];
        Buffer.BlockCopy(sendBuffer, 0, splitBuffer, 0, 5);

        // 解包
        this._ResolvePacket(splitBuffer);

        Debug.Log($"【分包模擬】模擬5秒後Server發送剩餘封包給Client");
        await Task.Delay(5000);

        // 製作分包，後14 Byte
        splitBuffer = new byte[14];
        Buffer.BlockCopy(sendBuffer, 5, splitBuffer, 0, 14);

        // 解包
        this._ResolvePacket(splitBuffer);
    }

    private async Task _SimulateStickPacketResolve()
    {
        Debug.Log("黏包模擬");

        var sendBuffer = this._Generate19BytePacket();

        // 製作黏包，前6 Byte
        byte[] stickBuffer = new byte[6];
        Buffer.BlockCopy(sendBuffer.ToArray(), 0, stickBuffer, 0, 5);

        // 加上黏包，首次封包共25 Byte
        sendBuffer.AddRange(stickBuffer);

        // 解包
        this._ResolvePacket(sendBuffer.ToArray());

        Debug.Log($"【黏包模擬】模擬5秒後Server發送剩餘封包給Client");
        await Task.Delay(5000);

        // 製作黏包，後13 Byte
        stickBuffer = new byte[13];
        Buffer.BlockCopy(sendBuffer.ToArray(), 6, stickBuffer, 0, 13);

        // 解包
        this._ResolvePacket(stickBuffer);
    }

    private async Task _SimulateSplitAndStickPacketResolve()
    {
        Debug.Log("分 + 黏包模擬");

        var sendBuffer = this._Generate19BytePacket();

        // 製作黏包，前6 Byte
        byte[] stickBuffer = new byte[6];
        Buffer.BlockCopy(sendBuffer.ToArray(), 0, stickBuffer, 0, 6);

        // 加上黏包，首次封包共25 Byte
        sendBuffer.AddRange(stickBuffer);

        // 解包
        this._ResolvePacket(sendBuffer.ToArray());

        Debug.Log($"【分 + 黏包模擬】模擬5秒後Server發送3 Byte分包給Client");
        await Task.Delay(5000);

        // 製作分包，7~9 Byte，共3 Byte
        var splitBuffer = new byte[3];
        Buffer.BlockCopy(sendBuffer.ToArray(), 6, splitBuffer, 0, 3);

        // 解包
        this._ResolvePacket(splitBuffer.ToArray());

        Debug.Log($"【分 + 黏包模擬】模擬5秒後Server發送剩餘分包給Client");
        await Task.Delay(5000);

        // 製作分包，10~19 Byte，共10 Byte
        splitBuffer = new byte[10];
        Buffer.BlockCopy(sendBuffer.ToArray(), 9, splitBuffer, 0, 10);

        // 解包
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

        // 複製到緩存封包內
        Buffer.BlockCopy(buffer, 0, this._tempBuffer, this._packetSizeLeft, buffer.Length);

        Debug.Log($"複製網路封包至封包緩存...放置的起始位置 : {this._packetSizeLeft}, 放置的Byte數量 : {buffer.Length}");

        this._packetSizeLeft += buffer.Length;

        Debug.Log($"當前剩餘封包數量 : {this._packetSizeLeft}");

        int bufferReadPos = 0;
        int resolveNum = 0;

        while (this._packetSizeLeft > 0)
        {
            // 計算讀取的數量
            int bufferReadedSize = this._ReadPacket(this._tempBuffer, this._packetSizeLeft, bufferReadPos);

            if (bufferReadedSize > 0)
            {
                // 增加已讀取的封包Size至Pos
                bufferReadPos += bufferReadedSize;

                // 將Buffer Offset扣除掉已讀取的封包數量
                this._packetSizeLeft -= bufferReadedSize;

                Debug.Log($"讀取到的Packet數量 : {bufferReadedSize}, 剩餘的封包數量 : {this._packetSizeLeft}, 讀取Pos : {bufferReadPos}");

                resolveNum++;

                if (this._packetSizeLeft == 0)
                {
                    Debug.Log($"<color=#1ACB00>封包已完整解析，停止解析</color>");
                    break;
                }
                else
                {
                    Debug.Log($"<color=#FF8400>判斷已黏包，剩餘要讀取的數量 : {this._packetSizeLeft}，將進行下一次迴圈作解包</color>");
                }
            }
            else if (bufferReadedSize == 0)
            {
                // 如果BufferReadPos > 0，則代表剩餘的封包並不在Buffer最前端，將其移動到最前端，以利下一次封包讀取處理
                if (bufferReadPos > 0)
                {
                    // 判斷已分包，將剩餘的Buffer全數移至最前端，並且break掉迴圈，等待下一個封包的到來...
                    BufferTool.MoveBufferToFront(this._tempBuffer, bufferReadPos, this._packetSizeLeft);

                    Debug.Log($"<color=#FF8400>判斷已分包...且ReadPos > 0，將移動Buffer內資料至最前端 : 移動起始Pos : {bufferReadPos}, 移動Byte數量 : {this._packetSizeLeft}</color>");
                }
                else
                {
                    Debug.Log($"<color=#FF8400>判斷已分包，ReadPos = 0，無需移動Buffer至前端</color>");
                }

                Debug.Log($"將等待下一次封包的到來...");
                break;
            }
        }

        Debug.Log($"<color=#1ACB00>封包完整解析的數量 : {resolveNum}</color>");
    }

    private int _ReadPacket(byte[] buffer, int packetSizeLeft, int readPos)
    {
        // 不足標頭長度，不予以解析
        if (packetSizeLeft < PacketResolve._HEADER_LEN)
        {
            Debug.Log($"<color=#FF8400>封包長度不足定義標頭長度 => Actual Length : {packetSizeLeft}, less than required Header Length : {PacketResolve._HEADER_LEN}, needs more packet</color>");
            return 0;
        }

        int pos = readPos;
        Debug.Log($"當前讀取Pos : {pos}");

        // 取出頭2 Byte，得出長度資訊
        short length = BufferTool.GetShort(buffer, ref pos);

        // 實際接收到的封包資訊 < 解析出的長度資訊 = 分包
        if (packetSizeLeft < length)
        {
            Debug.Log($"<color=#FF8400>分包 => Expected Length : {length}, Actual Length : {packetSizeLeft}, needs more packet</color>");
            return 0;
        }
        // 封包足夠，後續解析有2種情況
        // 1. PacketLeftSize = Length >>> 完整解包
        // 2. PacketLeftSize > Length >>> 黏包
        else
        {
            Debug.Log($"<color=#1ACB00>封包足夠，進行寫入至Client解包類別</color>");
            this._WriteIntoNetProtocolResolver(buffer);
        }

        return length;
    }

    private void _WriteIntoNetProtocolResolver(byte[] buffer)
    {
        // 正式流程應是透過解出的FuncId取得Client對應的解包類別並做解包

        // 此處只是簡單模擬封包資料的取出
        int pos = 0;
        Debug.Log($"<color=#00C4D1>解包 Length : {BufferTool.GetShort(buffer, ref pos)}</color>");
        Debug.Log($"<color=#00C4D1>解包 FuncId : {BufferTool.GetShort(buffer, ref pos)}</color>");
        Debug.Log($"<color=#00C4D1>解包 ProtocolType : {BufferTool.GetShort(buffer, ref pos)}</color>");

        byte d1 = BufferTool.GetByte(buffer, ref pos);
        long d2 = BufferTool.GetLong(buffer, ref pos);
        short d3 = BufferTool.GetShort(buffer, ref pos);

        Debug.Log($"<color=#00C4D1>封包資料解析 D1 : {d1}, D2 : {d2}, D3 : {d3}</color>"); 
    }
}
