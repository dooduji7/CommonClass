using System;
using System.Text;
using ToolHandler.Core;

namespace ToolHandler.Estic
{
    public class EsticController
        : SocketToolControllerBase
    {
        private bool _waitingResultSubscribeAck;

        public override ToolType ToolType
        {
            get
            {
                return ToolHandler.Core.ToolType.Estic;
            }
        }

        public event EventHandler<
            ToolResultEventArgs<EsticResultData>>
            ResultReceived;

        public EsticController(
            ToolOptions options)
            : base(options)
        {
        }

        protected override bool OnSocketConnected()
        {
            _waitingResultSubscribeAck = false;

            return Send(
                EsticProtocol.CreateCommunicationStart());
        }

        protected override bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            return EsticProtocol.TryExtractFrame(
                buffer,
                out frame);
        }

        protected override void ProcessFrame(
            string frame)
        {
            string mid;

            if (!EsticProtocol.TryGetMid(
                frame,
                out mid))
            {
                OnError(
                    "Estic 수신 전문의 MID를 확인할 수 없습니다.");

                return;
            }

            switch (mid)
            {
                case "0002":
                    ProcessCommunicationStartAck();
                    break;

                case "0004":
                    ProcessCommandError(frame);
                    break;

                case "0005":
                    ProcessCommandAccepted(frame);
                    break;

                case "0061":
                    ProcessResult(frame);
                    break;

                case "9999":
                    break;
            }
        }

        protected override void OnKeepAlive()
        {
            Send(
                EsticProtocol.CreateKeepAlive());
        }

        protected override void OnSocketDisconnected(
            bool isStopping)
        {
            _waitingResultSubscribeAck = false;
        }

        private void ProcessCommunicationStartAck()
        {
            _waitingResultSubscribeAck = true;

            if (!Send(
                EsticProtocol.CreateResultSubscribe()))
            {
                _waitingResultSubscribeAck = false;
                RequestReconnect();
            }
        }

        private void ProcessCommandAccepted(
            string frame)
        {
            string acceptedMid;

            if (!EsticProtocol.TryGetAcceptedMid(
                frame,
                out acceptedMid))
            {
                OnError(
                    "Estic MID0005 승인 대상 MID를 확인할 수 없습니다. Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            if (!_waitingResultSubscribeAck)
            {
                OnError(
                    "Estic 대기 중인 Subscribe가 없는 상태에서 MID0005를 수신했습니다. " +
                    "AcceptedMID=" +
                    acceptedMid);

                return;
            }

            if (acceptedMid != "0060")
            {
                OnError(
                    "Estic result subscribe ACK MID가 일치하지 않습니다. " +
                    "Expected=0060, Actual=" +
                    acceptedMid +
                    ", Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            _waitingResultSubscribeAck = false;
            SetCommunicating();
        }

        private void ProcessCommandError(
            string frame)
        {
            if (_waitingResultSubscribeAck)
            {
                _waitingResultSubscribeAck = false;

                OnError(
                    "Estic result subscribe command rejected(MID0004). Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            OnError(
                "Estic command error(MID0004). Source=" +
                frame);
        }

        private void ProcessResult(
            string frame)
        {
            EsticResultData result;
            string errorMessage;

            if (!EsticProtocol.TryParseResult(
                frame,
                out result,
                out errorMessage))
            {
                OnError(errorMessage);
                return;
            }

            if (!Send(
                EsticProtocol.CreateResultAck()))
            {
                RequestReconnect();
                return;
            }

            OnResultReceived(result);
        }

        private void OnResultReceived(
            EsticResultData result)
        {
            EventHandler<
                ToolResultEventArgs<EsticResultData>>
                handler = ResultReceived;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolResultEventArgs<EsticResultData>(
                        result));
            }
        }
    }
}
