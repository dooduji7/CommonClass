using System;
using System.Text;
using ToolHandler.Core;

namespace ToolHandler.Yokoda
{
    public class YokodaController
        : SocketToolControllerBase
    {
        public override ToolType ToolType
        {
            get
            {
                return ToolHandler.Core.ToolType.Yokoda;
            }
        }

        public event EventHandler<
            ToolResultEventArgs<YokodaResultData>>
            ResultReceived;

        public YokodaController(
            ToolOptions options)
            : base(options)
        {
        }

        protected override bool OnSocketConnected()
        {
            // Yokoda Legacy Protocol에는 별도의
            // Communication Start / Subscribe 절차가 없다.
            SetCommunicating();
            return true;
        }

        protected override bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            return YokodaProtocol.TryExtractFrame(
                buffer,
                out frame);
        }

        protected override void ProcessFrame(
            string frame)
        {
            YokodaResultData result;
            string errorMessage;

            if (!YokodaProtocol.TryParseResult(
                frame,
                out result,
                out errorMessage))
            {
                OnError(errorMessage);
                return;
            }

            OnResultReceived(result);
        }

        protected override void OnKeepAlive()
        {
            // Legacy Yokoda에는 KeepAlive 송신 명령이 없다.
            // Socket 연결 상태는 SocketToolControllerBase에서 관리한다.
        }

        private void OnResultReceived(
            YokodaResultData result)
        {
            EventHandler<
                ToolResultEventArgs<YokodaResultData>>
                handler = ResultReceived;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolResultEventArgs<YokodaResultData>(
                        result));
            }
        }
    }
}
