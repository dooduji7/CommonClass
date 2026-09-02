using System;
using System.Collections.Generic;

namespace MessageSocket
{
    /// <summary>
    /// TCP Receive 호출 경계와 전문 경계를 분리하기 위한 누적 Buffer.
    /// </summary>
    internal sealed class ReceiveBuffer
    {
        private readonly List<byte> buffer = new List<byte>();

        public int Count
        {
            get { return buffer.Count; }
        }

        public void Append(byte[] data, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (count < 0 || count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
                buffer.Add(data[i]);
        }

        public byte[] ToArray()
        {
            return buffer.ToArray();
        }

        public byte[] Take(int count)
        {
            if (count < 0 || count > buffer.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            byte[] result = buffer.GetRange(0, count).ToArray();
            buffer.RemoveRange(0, count);
            return result;
        }

        public void Clear()
        {
            buffer.Clear();
        }
    }
}
