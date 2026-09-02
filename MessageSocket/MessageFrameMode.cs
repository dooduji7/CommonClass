namespace MessageSocket
{
    /// <summary>
    /// 수신 byte stream에서 전문의 경계를 판별하는 기본 방식.
    /// </summary>
    public enum MessageFrameMode
    {
        Delimiter,
        FixedLength,
        LengthField
    }
}
