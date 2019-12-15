using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SAEANetworking
{

    /// <summary>
    /// Socket消息处理工具类
    /// </summary>
    public static class SAEAMessageTools
    {

        /// <summary>
        /// 将int类型序列化为字节数组
        /// </summary>
        /// <param name="value"></param>
        public static byte[] SerializeIntToByte(int value)
        {
            byte[] intByte = BitConverter.GetBytes(value);
            return intByte;
        }

        /// <summary>
        /// 从数组指定位置开始将其后面四个字节反序列化为int32
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static int DeserializeByteToInt(byte[] bytes, int startIndes = 0)
        {
            int i = BitConverter.ToInt32(bytes, startIndes);
            return i;
        }

        /// <summary>
        /// 将Uint类型序列化为字节数组
        /// </summary>
        public static byte[] SerializeUintToByte(uint value)
        {
            byte[] intByte = BitConverter.GetBytes(value);
            return intByte;
        }

        /// <summary>
        /// 从数组指定位置开始将其后面四个字节反序列化为Uint32
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static uint DeserializeByteToUint(byte[] bytes, int startIndes = 0)
        {
            uint i = BitConverter.ToUInt32(bytes, startIndes);
            return i;
        }

        /// <summary>
        /// 将Short类型序列化为字节数组
        /// </summary>
        public static byte[] SerializeShortToByte(short value)
        {
            byte[] intByte = BitConverter.GetBytes(value);
            return intByte;
        }

        /// <summary>
        /// 从数组指定位置开始将其后面两个字节反序列化为Short
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static short DeserializeByteToShort(byte[] bytes, int startIndes = 0)
        {
            short i = BitConverter.ToInt16(bytes, startIndes);
            return i;
        }

        /// <summary>
        /// 将String类型的数据序列化为字节数组
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] serializeStringToByte(string str)
        {
            byte[] strByte = System.Text.Encoding.UTF8.GetBytes(str);
            return strByte;
        }

        /// <summary>
        /// 从给定的位置和长度将字节数组反序列化为String类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DeserializeByteToString(byte[] bytes, int index = 0, int count = -1)
        {
            if (bytes == null) return null;
            if (count == -1) count = bytes.Length;
            string str = System.Text.Encoding.UTF8.GetString(bytes, index, count);
            return str;
        }

        /// <summary>
        /// 将对象序列化为字节数组
        /// </summary>
        /// <param name="messageData"></param>
        /// <returns></returns>
        public static byte[] SerializeMessageToByte(object messageData)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream memory = new MemoryStream();
            bf.Serialize(memory, messageData);
            byte[] bytes = memory.GetBuffer();
            memory.Close();
            return bytes;
        }

        ///// <summary>
        ///// 将字节数组反序列化为指定对象
        ///// </summary>
        ///// <param name="messageData"></param>
        ///// <returns></returns>
        public static T DeserializeByteToMessage<T>(byte[] bytes) where T : class
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream memory = new MemoryStream(bytes);
            T ss = (T)bf.Deserialize(memory);
            memory.Close();
            return ss;
        }



        /// <summary>
        /// 根据指定的长度切割指定的数组，并将切割好的数组按顺序放入给定的队列中
        /// </summary>
        public static Queue<byte[]> SplitByteToContainer(byte[] byteArr, int buffer, Queue<byte[]> container)
        {
            if (container == null) return null;
            if (byteArr == null || byteArr.Length == 0) return container;

            //当前数组剩余数据
            int curResidue = byteArr.Length;
            //当前已切割数据长度
            int curSplit = 0;

            while (curResidue > 0)
            {
                if (buffer > curResidue)
                {
                    buffer = curResidue;
                }

                byte[] tempByte = new byte[buffer];
                System.Array.Copy(byteArr, curSplit, tempByte, 0, buffer);

                curSplit += buffer;
                curResidue -= buffer;

                container.Enqueue(tempByte);
            }

            return container;
        }

        /// <summary>
        /// 根据指定的长度切割指定的数组，并将切割好的数组按顺序放入给定的List列表中
        /// </summary>
        public static List<byte[]> SplitByteToContainer(byte[] byteArr, int buffer, List<byte[]> container)
        {
            if (container == null) return null;
            if (byteArr == null || byteArr.Length == 0) return container;

            //当前数组剩余数据
            int curResidue = byteArr.Length;
            //当前已切割数据长度
            int curSplit = 0;

            while (curResidue > 0)
            {
                if (buffer > curResidue)
                {
                    buffer = curResidue;
                }

                byte[] tempByte = new byte[buffer];
                System.Array.Copy(byteArr, curSplit, tempByte, 0, buffer);

                curSplit += buffer;
                curResidue -= buffer;

                container.Add(tempByte);
            }

            return container;
        }

        /// <summary>
        /// 为消息数组增加一个长度标记
        /// </summary>
        public static byte[] AddLengthMarkerForMassage(byte[] message, bool isOriginal = true)
        {
            if (message == null) return null;
            int messageLength;
            List<byte> tempMesaage = new List<byte>();
            if (isOriginal)
            {
                messageLength = message.Length;
            }
            else
            {
                messageLength = message.Length + 4;
            }

            byte[] newMessage = ConcatByte(SerializeIntToByte(messageLength), message);

            return newMessage;
        }

        /// <summary>
        /// 将两个数组拼接
        /// </summary>
        /// <param name="byte1"></param>
        /// <param name="byte2"></param>
        /// <returns></returns>
        public static byte[] ConcatByte(byte[] byte1, byte[] byte2)
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(byte1);
            temp.AddRange(byte2);
            return temp.ToArray();
        }

        /// <summary>
        /// 将三个数组拼接
        /// </summary>
        /// <param name="byte1"></param>
        /// <param name="byte2"></param>
        /// <returns></returns>
        public static byte[] ConcatByte(byte[] byte1, byte[] byte2, byte[] byte3)
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(byte1);
            temp.AddRange(byte2);
            temp.AddRange(byte3);
            return temp.ToArray();
        }

        /// <summary>
        /// 根据指定的起始位置和长度从数组中截取数据，返回一个新数组
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static byte[] CutByte(byte[] bytes, int startIndex, int count)
        {
            //如果传入的参数不符合条件则返回长度为0的数组
            byte[] temp;
            if (bytes == null || bytes.Length == 0 || startIndex >= bytes.Length || startIndex < 0 || count > bytes.Length - startIndex || count < 0)
            {
                temp = new byte[0];
            }
            temp = bytes.Skip(startIndex).Take(count).ToArray();
            return temp;
        }

    }


}