using System;
using System.Collections.Generic;
using System.Text;

namespace ToolHandler.Atlas
{
    public static class AtlasProtocol
    {
        private const int HeaderLength = 8;
        private const int MinimumTorqueResultLength = 238;

        public static string CreateCommunicationStart()
        {
            return "002000010010        " + '\0';
        }

        public static string CreateResultSubscribe()
        {
            return "002000600050        " + '\0';
        }

        public static string CreateResultAck()
        {
            return "002000620050        " + '\0';
        }

        public static string CreateKeepAlive()
        {
            return "002099990010        " + '\0';
        }

        public static string CreateMid0036Response()
        {
            return "002000360050        " + '\0';
        }

        public static bool TryCreateJobCommands(
            string jobNo,
            out string[] commands,
            out string errorMessage)
        {
            commands = null;
            errorMessage = string.Empty;

            int jobValue;

            if (string.IsNullOrWhiteSpace(jobNo) ||
                !int.TryParse(jobNo, out jobValue) ||
                jobValue < 0 ||
                jobValue > 9)
            {
                errorMessage =
                    "Atlas JobNo는 Legacy 전문 형식상 0~9 한 자리 값이어야 합니다.";

                return false;
            }

            commands = new string[]
            {
                "002000300010        " + '\0',
                "002101300010        0" + '\0',
                "002101300010        1" + '\0',
                "002200380010        0" + jobValue.ToString() + '\0'
            };

            return true;
        }

        public static bool TryCreatePSetCommand(
            string psetNo,
            out string command,
            out string errorMessage)
        {
            command = string.Empty;
            errorMessage = string.Empty;

            int psetValue;

            if (string.IsNullOrWhiteSpace(psetNo) ||
                !int.TryParse(psetNo, out psetValue) ||
                psetValue < 0 ||
                psetValue > 999)
            {
                errorMessage =
                    "Atlas PSetNo는 0~999 범위의 숫자여야 합니다.";

                return false;
            }

            command =
                "002300180010        " +
                psetValue.ToString("D3") +
                '\0';

            return true;
        }

        public static bool TryCreateVinCommand(
            string vin,
            out string command,
            out string errorMessage)
        {
            command = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(vin))
            {
                errorMessage = "Atlas VIN 값이 비어 있습니다.";
                return false;
            }

            // Legacy 구현은 전문 Length를 20 + VIN.Length로 계산했다.
            // Open Protocol 4자리 Length field 범위를 초과하지 않도록 제한한다.
            if (vin.Length > 9979)
            {
                errorMessage = "Atlas VIN 값이 너무 깁니다.";
                return false;
            }

            int messageLength = 20 + vin.Length;

            command =
                messageLength.ToString("D4") +
                "0050001       00" +
                vin +
                '\0';

            return true;
        }

        public static bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            frame = string.Empty;

            if (buffer == null)
                return false;

            while (true)
            {
                RemoveLeadingTerminators(buffer);

                if (buffer.Length < 4)
                    return false;

                int messageLength;

                if (!int.TryParse(
                    buffer.ToString(0, 4),
                    out messageLength))
                {
                    buffer.Remove(0, 1);
                    continue;
                }

                if (messageLength < HeaderLength)
                {
                    buffer.Remove(0, 1);
                    continue;
                }

                if (buffer.Length < messageLength)
                    return false;

                frame = buffer.ToString(0, messageLength);
                buffer.Remove(0, messageLength);

                // Length에는 종단 NULL이 포함되지 않는다.
                RemoveLeadingTerminators(buffer);

                return true;
            }
        }

        public static bool TryGetMid(
            string frame,
            out string mid)
        {
            mid = string.Empty;

            if (string.IsNullOrEmpty(frame) ||
                frame.Length < HeaderLength)
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

            // Open Protocol MID0005:
            // Byte 21~24 (1-base) = accepted request MID.
            // C# index 기준으로는 20, length 4.
            if (frame.Length < 24)
                return false;

            acceptedMid =
                frame.Substring(20, 4);

            return true;
        }

        public static bool TryParseTorqueResult(
            string frame,
            out TorqueResultData result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;

            string mid;

            if (!TryGetMid(frame, out mid))
            {
                errorMessage = "Atlas MID를 확인할 수 없습니다.";
                return false;
            }

            if (mid != "0061")
            {
                errorMessage = "Atlas Torque Result MID0061 전문이 아닙니다.";
                return false;
            }

            if (frame.Length < MinimumTorqueResultLength)
            {
                errorMessage =
                    "Atlas MID0061 전문 길이가 부족합니다. Length=" +
                    frame.Length.ToString();

                return false;
            }

            string torque;
            string torqueMin;
            string torqueMax;

            if (!TryConvertTorque(frame.Substring(183, 6), out torque))
            {
                errorMessage = "Atlas Torque 값 변환에 실패했습니다.";
                return false;
            }

            if (!TryConvertTorque(frame.Substring(159, 6), out torqueMin))
            {
                errorMessage = "Atlas TorqueMin 값 변환에 실패했습니다.";
                return false;
            }

            if (!TryConvertTorque(frame.Substring(167, 6), out torqueMax))
            {
                errorMessage = "Atlas TorqueMax 값 변환에 실패했습니다.";
                return false;
            }

            TorqueResultData data = new TorqueResultData();

            data.Mid = mid;
            data.PSet = frame.Substring(92, 3);
            data.JobNo = frame.Substring(86, 4);
            data.TotalResult = frame.Substring(120, 1);

            data.Torque = torque;
            data.TorqueMin = torqueMin;
            data.TorqueMax = torqueMax;
            data.TorqueResult = frame.Substring(126, 1);

            data.Angle = frame.Substring(212, 5);
            data.AngleMin = frame.Substring(191, 6);
            data.AngleMax = frame.Substring(198, 6);
            data.AngleResult = frame.Substring(129, 1);

            data.RundownAngle = frame.Substring(233, 5);
            data.RundownAngleMin = frame.Substring(219, 6);
            data.RundownAngleMax = frame.Substring(226, 6);
            data.RundownAngleResult = frame.Substring(132, 1);

            if (frame.Length >= 419)
                data.ResultType = frame.Substring(417, 2);

            data.SourceData = frame;

            result = data;
            return true;
        }

        private static bool TryConvertTorque(
            string torqueString,
            out string torqueValue)
        {
            torqueValue = string.Empty;

            if (string.IsNullOrEmpty(torqueString) ||
                torqueString.Length != 6)
            {
                return false;
            }

            char[] chars = torqueString.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                    continue;

                if (chars[i] == '.')
                {
                    chars[i] = '0';
                    continue;
                }

                return false;
            }

            string normalized = new string(chars);

            torqueValue =
                normalized.Substring(0, 4) + "." +
                normalized.Substring(4, 2);

            return true;
        }

        private static void RemoveLeadingTerminators(
            StringBuilder buffer)
        {
            while (buffer.Length > 0)
            {
                char value = buffer[0];

                if (value != '\0' &&
                    value != '\r' &&
                    value != '\n')
                {
                    break;
                }

                buffer.Remove(0, 1);
            }
        }
    }
}
