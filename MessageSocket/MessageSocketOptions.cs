using System;
using System.Text;

namespace MessageSocket
{
    /// <summary>
    /// MessageSocket 통신 및 수신 전문 분리 설정.
    /// </summary>
    public class MessageSocketOptions
    {
        public MessageSocketOptions()
        {
            Host = "127.0.0.1";
            Encoding = Encoding.ASCII;
            FrameMode = MessageFrameMode.Delimiter;
            Delimiter = new byte[] { 0x0D, 0x0A };
            IncludeDelimiter = true;
            FixedFrameLength = 1;
            LengthFieldOffset = 0;
            LengthFieldSize = 4;
            LengthFieldBigEndian = true;
            LengthFieldAdjustment = 0;
            MaxFrameLength = 1024 * 1024;
            MaxBufferLength = 2 * 1024 * 1024;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public Encoding Encoding { get; set; }
        public MessageFrameMode FrameMode { get; set; }

        // Delimiter 방식
        public byte[] Delimiter { get; set; }
        public bool IncludeDelimiter { get; set; }

        // FixedLength 방식
        public int FixedFrameLength { get; set; }

        // LengthField 방식
        // 전체 전문 길이 = LengthField에서 읽은 값 + LengthFieldAdjustment
        public int LengthFieldOffset { get; set; }
        public int LengthFieldSize { get; set; }
        public bool LengthFieldBigEndian { get; set; }
        public int LengthFieldAdjustment { get; set; }

        // 비정상 길이 값 또는 무한 누적을 방지하기 위한 안전 한계
        public int MaxFrameLength { get; set; }
        public int MaxBufferLength { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentException("Host가 비어 있습니다.", nameof(Host));

            if (Port < 1 || Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port));

            if (Encoding == null)
                throw new ArgumentNullException(nameof(Encoding));

            if (MaxFrameLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxFrameLength));

            if (MaxBufferLength < MaxFrameLength)
                throw new ArgumentException("MaxBufferLength는 MaxFrameLength 이상이어야 합니다.");

            if (FrameMode == MessageFrameMode.Delimiter)
            {
                if (Delimiter == null || Delimiter.Length == 0)
                    throw new ArgumentException("Delimiter 방식에서는 Delimiter가 필요합니다.");
            }
            else if (FrameMode == MessageFrameMode.FixedLength)
            {
                if (FixedFrameLength <= 0 || FixedFrameLength > MaxFrameLength)
                    throw new ArgumentOutOfRangeException(nameof(FixedFrameLength));
            }
            else if (FrameMode == MessageFrameMode.LengthField)
            {
                if (LengthFieldOffset < 0)
                    throw new ArgumentOutOfRangeException(nameof(LengthFieldOffset));

                if (LengthFieldSize != 1 && LengthFieldSize != 2 && LengthFieldSize != 4)
                    throw new ArgumentException("LengthFieldSize는 1, 2, 4 byte만 지원합니다.");

                if (LengthFieldOffset + LengthFieldSize > MaxFrameLength)
                    throw new ArgumentException("LengthField 위치가 MaxFrameLength 범위를 초과합니다.");
            }
        }
    }
}
