using System;
using System.Text;

namespace ToolHandler.Atlas
{
    public static class AtlasPowerMacMultiProtocol
    {
        private const int SpindleCountPosition = 163;
        private const int SpindleCountLength = 2;

        private const int SpindleDataStart = 174;
        private const int SpindleDataLength = 67;

        private const int TorqueResultOffset = 0;
        private const int AngleResultOffset = 3;
        private const int TorqueOffset = 6;
        private const int TorqueLength = 7;
        private const int AngleOffset = 15;
        private const int AngleLength = 7;

        public static string CreateCommunicationStart()
        {
            return AtlasProtocol.CreateCommunicationStart();
        }

        public static string CreateResultSubscribe()
        {
            return "002001050010        " + '\0';
        }

        public static string CreateResultAck()
        {
            return "002001080010        " + '\0';
        }

        public static string CreateKeepAlive()
        {
            // Legacy PowerMac Multi는 MID9999 Revision 005를 사용한다.
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
                    "Atlas PowerMac Multi MID를 확인할 수 없습니다.";

                return false;
            }

            if (mid != "0106")
            {
                errorMessage =
                    "Atlas PowerMac Multi Result MID0106 전문이 아닙니다.";

                return false;
            }

            if (frame.Length <
                SpindleCountPosition + SpindleCountLength)
            {
                errorMessage =
                    "Atlas PowerMac Multi MID0106 전문 길이가 부족합니다. Length=" +
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
                    "Atlas PowerMac Multi Spindle Count를 변환할 수 없습니다. Value=" +
                    countText;

                return false;
            }

            if (spindleCount < 0)
            {
                errorMessage =
                    "Atlas PowerMac Multi Spindle Count가 잘못되었습니다.";

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
                AngleOffset +
                AngleLength;

            if (frame.Length < requiredLength)
            {
                errorMessage =
                    "Atlas PowerMac Multi MID0106 Spindle 데이터 길이가 부족합니다. " +
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

                TorqueResultData data =
                    new TorqueResultData();

                data.Mid = mid;

                data.TorqueResult =
                    frame.Substring(
                        offset + TorqueResultOffset,
                        1);

                data.AngleResult =
                    frame.Substring(
                        offset + AngleResultOffset,
                        1);

                data.Torque =
                    frame.Substring(
                        offset + TorqueOffset,
                        TorqueLength)
                        .Trim();

                data.Angle =
                    frame.Substring(
                        offset + AngleOffset,
                        AngleLength)
                        .Trim();

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
