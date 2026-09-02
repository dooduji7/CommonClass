using System;
using System.Collections.Generic;
using System.Text;

namespace ToolHandler.Atlas
{
    public static class AtlasPowerFocusMultiProtocol
    {
        private const int SpindleCountPosition = 22;
        private const int SpindleCountLength = 2;

        private const int SpindleDataStart = 178;
        private const int SpindleDataLength = 18;

        public static string CreateCommunicationStart()
        {
            return AtlasProtocol.CreateCommunicationStart();
        }

        public static string CreateResultSubscribe()
        {
            return "002001000010        " + '\0';
        }

        public static string CreateResultAck()
        {
            return "002001020010        " + '\0';
        }

        public static string CreateKeepAlive()
        {
            // Legacy PowerFocus Multi는 MID9999 Revision 005를 사용한다.
            return "002099990050        " + '\0';
        }

        public static bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            return AtlasProtocol.TryExtractFrame(
                buffer,
                out frame);
        }

        public static bool TryGetMid(
            string frame,
            out string mid)
        {
            return AtlasProtocol.TryGetMid(
                frame,
                out mid);
        }

        public static bool TryParseTorqueResults(
            string frame,
            out TorqueResultData[] results,
            out string errorMessage)
        {
            results = null;
            errorMessage = string.Empty;

            string mid;

            if (!TryGetMid(
                frame,
                out mid))
            {
                errorMessage =
                    "Atlas PowerFocus Multi MID를 확인할 수 없습니다.";

                return false;
            }

            if (mid != "0101")
            {
                errorMessage =
                    "Atlas PowerFocus Multi Result MID0101 전문이 아닙니다.";

                return false;
            }

            if (frame.Length <
                SpindleCountPosition + SpindleCountLength)
            {
                errorMessage =
                    "Atlas PowerFocus Multi MID0101 전문 길이가 부족합니다. Length=" +
                    frame.Length.ToString();

                return false;
            }

            string countText =
                frame.Substring(
                    SpindleCountPosition,
                    SpindleCountLength)
                    .Trim();

            int spindleCount = 0;

            if (countText.Length > 0 &&
                !int.TryParse(
                    countText,
                    out spindleCount))
            {
                errorMessage =
                    "Atlas PowerFocus Multi Spindle Count를 변환할 수 없습니다. Value=" +
                    countText;

                return false;
            }

            if (spindleCount < 0)
            {
                errorMessage =
                    "Atlas PowerFocus Multi Spindle Count가 잘못되었습니다.";

                return false;
            }

            if (spindleCount == 0)
            {
                results =
                    new TorqueResultData[0];

                return true;
            }

            int requiredLength =
                SpindleDataStart +
                ((spindleCount - 1) * SpindleDataLength) +
                14;

            if (frame.Length < requiredLength)
            {
                errorMessage =
                    "Atlas PowerFocus Multi MID0101 Spindle 데이터 길이가 부족합니다. " +
                    "SpindleCount=" +
                    spindleCount.ToString() +
                    ", Length=" +
                    frame.Length.ToString() +
                    ", Required=" +
                    requiredLength.ToString();

                return false;
            }

            TorqueResultData[] parsed =
                new TorqueResultData[spindleCount];

            for (int index = 0;
                index < spindleCount;
                index++)
            {
                int offset =
                    SpindleDataStart +
                    (index * SpindleDataLength);

                string torqueInteger =
                    frame.Substring(
                        offset + 2,
                        4);

                string torqueDecimal =
                    frame.Substring(
                        offset + 6,
                        2);

                TorqueResultData data =
                    new TorqueResultData();

                data.Mid = mid;

                data.TorqueResult =
                    frame.Substring(
                        offset,
                        1);

                data.Torque =
                    torqueInteger +
                    "." +
                    torqueDecimal;

                data.Angle =
                    frame.Substring(
                        offset + 9,
                        5);

                // Legacy AtlasPowerFocusMulti 소스는 AngleResults 배열을
                // 생성하지만 값을 대입하지 않는다.
                // 정확한 위치 근거가 없으므로 임의 추정하지 않는다.
                data.AngleResult =
                    string.Empty;

                data.SourceData =
                    frame;

                parsed[index] =
                    data;
            }

            results = parsed;
            return true;
        }
    }
}
