using System;

namespace MessageSocket
{
    internal sealed class DelimiterFrameParser : IMessageFrameParser
    {
        private readonly byte[] delimiter;

        public DelimiterFrameParser(byte[] delimiter)
        {
            if (delimiter == null || delimiter.Length == 0)
                throw new ArgumentException("delimiter가 비어 있습니다.", nameof(delimiter));

            this.delimiter = new byte[delimiter.Length];
            Buffer.BlockCopy(delimiter, 0, this.delimiter, 0, delimiter.Length);
        }

        public bool TryGetFrameLength(byte[] buffer, int count, out int frameLength)
        {
            frameLength = 0;

            if (buffer == null || count < delimiter.Length)
                return false;

            int lastStart = count - delimiter.Length;

            for (int i = 0; i <= lastStart; i++)
            {
                bool matched = true;

                for (int j = 0; j < delimiter.Length; j++)
                {
                    if (buffer[i + j] != delimiter[j])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    frameLength = i + delimiter.Length;
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class FixedLengthFrameParser : IMessageFrameParser
    {
        private readonly int fixedLength;

        public FixedLengthFrameParser(int fixedLength)
        {
            if (fixedLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(fixedLength));

            this.fixedLength = fixedLength;
        }

        public bool TryGetFrameLength(byte[] buffer, int count, out int frameLength)
        {
            frameLength = fixedLength;
            return count >= fixedLength;
        }
    }

    internal sealed class LengthFieldFrameParser : IMessageFrameParser
    {
        private readonly int offset;
        private readonly int size;
        private readonly bool bigEndian;
        private readonly int adjustment;
        private readonly int maxFrameLength;

        public LengthFieldFrameParser(int offset, int size, bool bigEndian, int adjustment, int maxFrameLength)
        {
            this.offset = offset;
            this.size = size;
            this.bigEndian = bigEndian;
            this.adjustment = adjustment;
            this.maxFrameLength = maxFrameLength;
        }

        public bool TryGetFrameLength(byte[] buffer, int count, out int frameLength)
        {
            frameLength = 0;
            int required = offset + size;

            if (buffer == null || count < required)
                return false;

            uint lengthValue = 0;

            if (bigEndian)
            {
                for (int i = 0; i < size; i++)
                    lengthValue = (lengthValue << 8) | buffer[offset + i];
            }
            else
            {
                for (int i = size - 1; i >= 0; i--)
                    lengthValue = (lengthValue << 8) | buffer[offset + i];
            }

            long total = (long)lengthValue + adjustment;

            if (total < required)
                throw new InvalidOperationException("LengthField가 나타내는 전체 전문 길이가 Header 길이보다 작습니다.");

            if (total > maxFrameLength)
                throw new InvalidOperationException("전문 길이가 MaxFrameLength를 초과했습니다. Length=" + total);

            frameLength = (int)total;
            return count >= frameLength;
        }
    }
}
