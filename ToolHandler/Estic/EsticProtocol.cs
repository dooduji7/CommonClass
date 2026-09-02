using System;
using System.Text;

namespace ToolHandler.Estic
{
    public static class EsticProtocol
    {
        private const int HeaderLength = 20;

        public static string CreateCommunicationStart()
        {
            return "002000010010        " + '\0';
        }

        public static string CreateResultSubscribe()
        {
            // Legacy Estic MID0060 Revision 005
            return "002000600050        " + '\0';
        }

        public static string CreateResultAck()
        {
            // Legacy Estic MID0062 Revision 005
            return "002000620050        " + '\0';
        }

        public static string CreateKeepAlive()
        {
            // Legacy Estic MID9999 Revision 001
            return "002099990010        " + '\0';
        }

        public static bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            frame = string.Empty;

            if (buffer == null)
                return false;

            // 이전 전문 뒤에 남은 종료 문자를 제거한다.
            while (buffer.Length > 0 &&
                   (buffer[0] == '\0' ||
                    buffer[0] == '\r' ||
                    buffer[0] == '\n'))
            {
                buffer.Remove(0, 1);
            }

            if (buffer.Length < 4)
                return false;

            int messageLength;

            if (!int.TryParse(
                buffer.ToString(0, 4),
                out messageLength))
            {
                // Open Protocol 헤더가 아닌 데이터가 들어오면
                // 무한 대기를 피하기 위해 1바이트를 버린다.
                buffer.Remove(0, 1);
                return false;
            }

            if (messageLength < HeaderLength)
            {
                buffer.Remove(0, 1);
                return false;
            }

            if (buffer.Length < messageLength)
                return false;

            frame =
                buffer.ToString(
                    0,
                    messageLength);

            buffer.Remove(
                0,
                messageLength);

            // 길이 값에는 포함되지 않는 종료 문자를 소비한다.
            while (buffer.Length > 0 &&
                   (buffer[0] == '\0' ||
                    buffer[0] == '\r' ||
                    buffer[0] == '\n'))
            {
                buffer.Remove(0, 1);
            }

            return true;
        }

        public static bool TryGetMid(
            string frame,
            out string mid)
        {
            mid = string.Empty;

            if (string.IsNullOrEmpty(frame) ||
                frame.Length < 8)
            {
                return false;
            }

            mid = frame.Substring(4, 4);
            return true;
        }

        public static bool TryGetAcceptedMid(
            string frame,
            out string acceptedMid)
        {
            acceptedMid = string.Empty;

            string mid;

            if (!TryGetMid(
                frame,
                out mid))
            {
                return false;
            }

            if (mid != "0005")
                return false;

            // Open Protocol MID0005
            // Byte 21~24(1-base) = 승인된 요청 MID
            if (frame.Length < 24)
                return false;

            acceptedMid =
                frame.Substring(20, 4);

            return true;
        }

        public static bool TryParseResult(
            string frame,
            out EsticResultData result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;

            string mid;

            if (!TryGetMid(
                frame,
                out mid))
            {
                errorMessage =
                    "Estic MID를 확인할 수 없습니다.";

                return false;
            }

            if (mid != "0061")
            {
                errorMessage =
                    "Estic Result MID0061 전문이 아닙니다.";

                return false;
            }

            // Legacy parser:
            // PSet  = Substring(90, 3)
            // Result= Substring(107, 1)
            // Value = Substring(140, 3) + "." + Substring(143, 2)
            //
            // 마지막 접근 위치가 index 144이므로 최소 145자가 필요하다.
            if (frame.Length < 145)
            {
                errorMessage =
                    "Estic MID0061 전문 길이가 부족합니다. Length=" +
                    frame.Length.ToString() +
                    ", Required=145";

                return false;
            }

            EsticResultData parsed =
                new EsticResultData();

            parsed.PSet =
                frame.Substring(90, 3);

            parsed.Result =
                frame.Substring(107, 1);

            parsed.Value =
                frame.Substring(140, 3) +
                "." +
                frame.Substring(143, 2);

            result = parsed;
            return true;
        }
    }
}
