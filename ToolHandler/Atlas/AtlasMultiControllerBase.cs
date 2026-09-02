using System;
using System.Text;
using ToolHandler.Core;

namespace ToolHandler.Atlas
{
    public abstract class AtlasMultiControllerBase
        : SocketToolControllerBase
    {
        private bool _waitingResultSubscribeAck;

        protected abstract string ToolDisplayName
        {
            get;
        }

        protected abstract string ResultMid
        {
            get;
        }

        protected abstract string ResultSubscribeMid
        {
            get;
        }

        public event EventHandler<
            ToolResultEventArgs<TorqueResultData[]>>
            ResultReceived;

        protected AtlasMultiControllerBase(
            ToolOptions options)
            : base(options)
        {
        }

        protected abstract string CreateResultSubscribe();

        protected abstract string CreateResultAck();

        protected abstract string CreateKeepAlive();

        protected abstract bool TryParseTorqueResults(
            string frame,
            out TorqueResultData[] results,
            out string errorMessage);

        protected override bool OnSocketConnected()
        {
            _waitingResultSubscribeAck = false;

            return Send(
                AtlasProtocol.CreateCommunicationStart());
        }

        protected override bool TryExtractFrame(
            StringBuilder buffer,
            out string frame)
        {
            return AtlasProtocol.TryExtractFrame(
                buffer,
                out frame);
        }

        protected override void ProcessFrame(
            string frame)
        {
            string mid;

            if (!AtlasProtocol.TryGetMid(
                frame,
                out mid))
            {
                OnError(
                    ToolDisplayName +
                    " 수신 전문의 MID를 확인할 수 없습니다.");

                return;
            }

            if (mid == "0002")
            {
                ProcessCommunicationStartAck();
                return;
            }

            if (mid == "0004")
            {
                ProcessCommandError(frame);
                return;
            }

            if (mid == "0005")
            {
                ProcessCommandAccepted(frame);
                return;
            }

            if (mid == ResultMid)
            {
                ProcessTorqueResults(frame);
                return;
            }

            if (mid == "9999")
                return;
        }

        protected override void OnKeepAlive()
        {
            Send(
                CreateKeepAlive());
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
                CreateResultSubscribe()))
            {
                _waitingResultSubscribeAck = false;
                RequestReconnect();
            }
        }

        private void ProcessCommandAccepted(
            string frame)
        {
            string acceptedMid;

            if (!AtlasProtocol.TryGetAcceptedMid(
                frame,
                out acceptedMid))
            {
                OnError(
                    ToolDisplayName +
                    " MID0005 승인 대상 MID를 확인할 수 없습니다. Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            if (!_waitingResultSubscribeAck)
            {
                OnError(
                    ToolDisplayName +
                    " 대기 중인 Subscribe가 없는 상태에서 MID0005를 수신했습니다. " +
                    "AcceptedMID=" +
                    acceptedMid);

                return;
            }

            if (acceptedMid != ResultSubscribeMid)
            {
                OnError(
                    ToolDisplayName +
                    " result subscribe ACK MID가 일치하지 않습니다. " +
                    "Expected=" +
                    ResultSubscribeMid +
                    ", Actual=" +
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
                    ToolDisplayName +
                    " result subscribe command rejected(MID0004). Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            OnError(
                ToolDisplayName +
                " command error(MID0004). Source=" +
                frame);
        }

        private void ProcessTorqueResults(
            string frame)
        {
            TorqueResultData[] results;
            string errorMessage;

            if (!TryParseTorqueResults(
                frame,
                out results,
                out errorMessage))
            {
                OnError(errorMessage);
                return;
            }

            if (!Send(
                CreateResultAck()))
            {
                RequestReconnect();
                return;
            }

            OnResultReceived(results);
        }

        private void OnResultReceived(
            TorqueResultData[] results)
        {
            EventHandler<
                ToolResultEventArgs<TorqueResultData[]>>
                handler = ResultReceived;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolResultEventArgs<TorqueResultData[]>(
                        results));
            }
        }
    }
}
