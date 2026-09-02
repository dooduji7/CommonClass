namespace MessageSocket
{
    /// <summary>
    /// 누적 수신 Buffer의 시작 위치부터 완성된 전문 길이를 판별한다.
    /// 사용자 정의 전문 규격은 이 인터페이스를 구현하여 확장할 수 있다.
    /// </summary>
    public interface IMessageFrameParser
    {
        bool TryGetFrameLength(byte[] buffer, int count, out int frameLength);
    }
}
