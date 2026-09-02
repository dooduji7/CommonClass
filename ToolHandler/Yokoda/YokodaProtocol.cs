using System.Text;

namespace ToolHandler.Yokoda
{
    public static class YokodaProtocol
    {
        private const int MinimumFrameLength = 15;

        public static bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            frame = string.Empty;

            if (buffer == null)
                return false;

            // 이전 데이터 뒤에 남을 수 있는 NUL/CR을 정리한다.
            while (buffer.Length > 0 &&
                   (buffer[0] == '\0' ||
                    buffer[0] == '\r'))
            {
                buffer.Remove(0, 1);
            }

            if (buffer.Length < MinimumFrameLength)
                return false;

            // Legacy Yokoda에는 Open Protocol처럼 길이 헤더가 없다.
            // 가능한 경우 개행을 프레임 경계로 우선 사용한다.
            int newLineIndex = -1;

            for (int i = 0;
                i < buffer.Length;
                i++)
            {
                if (buffer[i] == '\n')
                {
                    newLineIndex = i;
                    break;
                }
            }

            if (newLineIndex >= 0 &&
                newLineIndex + 1 >= MinimumFrameLength)
            {
                int length =
                    newLineIndex + 1;

                frame =
                    buffer.ToString(
                        0,
                        length);

                buffer.Remove(
                    0,
                    length);

                return true;
            }

            // Legacy Controller 역시 Available >= 15인 시점의 수신 데이터를
            // 하나의 결과 전문으로 처리했다.
            // 종료 문자가 없는 장비 설정과의 호환을 위해 현재 누적 데이터
            // 전체를 한 프레임으로 처리한다.
            frame =
                buffer.ToString();

            buffer.Clear();

            return frame.Length >= MinimumFrameLength;
        }

        public static bool TryParseResult(
            string frame,
            out YokodaResultData result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(frame) ||
                frame.Length <= 14)
            {
                errorMessage =
                    "Yokoda 전문 길이가 부족합니다. Length=" +
                    (frame == null
                        ? "0"
                        : frame.Length.ToString()) +
                    ", Required=15";

                return false;
            }

            try
            {
                YokodaResultData parsed =
                    new YokodaResultData();

                // Legacy parser 그대로 유지
                parsed.WorkName =
                    frame.Substring(0, 1);

                parsed.Torque =
                    frame.Substring(8, 7);

                parsed.TotalResult =
                    frame.Length <= 17
                        ? "1"
                        : (frame[16] == '\n'
                            ? "1"
                            : "0");

                result = parsed;
                return true;
            }
            catch
            {
                errorMessage =
                    "Yokoda 결과 전문을 파싱할 수 없습니다. Source=" +
                    frame;

                return false;
            }
        }
    }
}
