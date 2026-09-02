using System;
using System.Collections.Generic;
using System.Text;
using ToolHandler.Core;

namespace ToolHandler.Atlas
{
    public class AtlasController
        : SocketToolControllerBase
    {
        private sealed class AtlasCommandRequest
        {
            public AtlasCommandType CommandType;
            public string Value;
            public string[] Commands;
            public int CommandIndex;
            public DateTime SentTime;
        }

        private readonly object _commandLock;
        private readonly Queue<AtlasCommandRequest> _commandQueue;

        private AtlasCommandRequest _currentCommand;
        private bool _waitingResultSubscribeAck;

        public override ToolType ToolType
        {
            get
            {
                return ToolHandler.Core.ToolType.Atlas;
            }
        }

        public int CommandTimeout { get; set; }

        public bool IsCommandPending
        {
            get
            {
                lock (_commandLock)
                {
                    return _currentCommand != null ||
                           _commandQueue.Count > 0;
                }
            }
        }

        public event EventHandler<ToolResultEventArgs<TorqueResultData>>
            ResultReceived;

        public event EventHandler<AtlasCommandEventArgs>
            CommandCompleted;

        public AtlasController(
            ToolOptions options)
            : base(options)
        {
            _commandLock = new object();
            _commandQueue = new Queue<AtlasCommandRequest>();
            CommandTimeout = 5000;
        }

        public bool SetJob(string jobNo)
        {
            string[] commands;
            string errorMessage;

            if (!AtlasProtocol.TryCreateJobCommands(
                jobNo,
                out commands,
                out errorMessage))
            {
                OnError(errorMessage);
                return false;
            }

            return EnqueueCommand(
                AtlasCommandType.Job,
                jobNo,
                commands);
        }

        public bool SetPSet(string psetNo)
        {
            string command;
            string errorMessage;

            if (!AtlasProtocol.TryCreatePSetCommand(
                psetNo,
                out command,
                out errorMessage))
            {
                OnError(errorMessage);
                return false;
            }

            return EnqueueCommand(
                AtlasCommandType.PSet,
                psetNo,
                new string[] { command });
        }

        public bool SetVin(string vin)
        {
            string command;
            string errorMessage;

            if (!AtlasProtocol.TryCreateVinCommand(
                vin,
                out command,
                out errorMessage))
            {
                OnError(errorMessage);
                return false;
            }

            return EnqueueCommand(
                AtlasCommandType.Vin,
                vin,
                new string[] { command });
        }

        protected override bool OnSocketConnected()
        {
            _waitingResultSubscribeAck = false;

            lock (_commandLock)
            {
                _currentCommand = null;
            }

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
                    "Atlas 수신 전문의 MID를 확인할 수 없습니다.");

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

                case "0035":
                    Send(
                        AtlasProtocol.CreateMid0036Response());
                    break;

                case "0061":
                    ProcessTorqueResult(frame);
                    break;

                case "9999":
                    break;
            }
        }

        protected override void OnKeepAlive()
        {
            CheckCommandTimeout();

            Send(
                AtlasProtocol.CreateKeepAlive());
        }

        protected override void OnReceiveTimeout()
        {
            CheckCommandTimeout();
        }

        protected override void OnSocketDisconnected(
            bool isStopping)
        {
            _waitingResultSubscribeAck = false;

            FailAllPendingCommands(
                isStopping
                    ? "Atlas communication stopped."
                    : "Atlas connection lost. Pending commands were canceled.");
        }

        private void ProcessCommunicationStartAck()
        {
            _waitingResultSubscribeAck = true;

            if (!Send(
                AtlasProtocol.CreateResultSubscribe()))
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
                    "Atlas MID0005 승인 대상 MID를 확인할 수 없습니다. Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            if (_waitingResultSubscribeAck)
            {
                if (acceptedMid != "0060")
                {
                    OnError(
                        "Atlas result subscribe ACK MID가 일치하지 않습니다. " +
                        "Expected=0060, Actual=" +
                        acceptedMid +
                        ", Source=" +
                        frame);

                    RequestReconnect();
                    return;
                }

                _waitingResultSubscribeAck = false;
                SetCommunicating();
                TryStartNextCommand();
                return;
            }

            AtlasCommandRequest completedRequest = null;
            AtlasCommandRequest nextStepRequest = null;
            string validationError = string.Empty;
            bool requestReconnect = false;

            lock (_commandLock)
            {
                if (_currentCommand == null)
                {
                    validationError =
                        "Atlas 대기 명령이 없는 상태에서 MID0005를 수신했습니다. AcceptedMID=" +
                        acceptedMid;
                }
                else
                {
                    string expectedMid;

                    if (!TryGetCommandMid(
                        _currentCommand,
                        out expectedMid))
                    {
                        validationError =
                            "Atlas 현재 명령의 MID를 확인할 수 없습니다.";

                        requestReconnect = true;
                    }
                    else if (acceptedMid != expectedMid)
                    {
                        validationError =
                            "Atlas command ACK MID가 일치하지 않습니다. " +
                            "Expected=" +
                            expectedMid +
                            ", Actual=" +
                            acceptedMid +
                            ", Source=" +
                            frame;

                        requestReconnect = true;
                    }
                    else
                    {
                        _currentCommand.CommandIndex++;

                        if (_currentCommand.CommandIndex >=
                            _currentCommand.Commands.Length)
                        {
                            completedRequest = _currentCommand;
                            _currentCommand = null;
                        }
                        else
                        {
                            nextStepRequest = _currentCommand;
                        }
                    }
                }
            }

            // Event callback에서 SetJob/SetPSet 등을 다시 호출할 수 있으므로
            // _commandLock 내부에서는 OnError / CommandCompleted를 발생시키지 않는다.
            if (!string.IsNullOrEmpty(validationError))
            {
                OnError(validationError);

                if (requestReconnect)
                    RequestReconnect();

                return;
            }

            if (completedRequest != null)
            {
                RaiseCommandCompleted(
                    completedRequest,
                    true,
                    "Atlas command accepted.");

                TryStartNextCommand();
                return;
            }

            if (nextStepRequest != null)
                SendCurrentCommandStep(nextStepRequest);
        }

        private void ProcessCommandError(
            string frame)
        {
            if (_waitingResultSubscribeAck)
            {
                _waitingResultSubscribeAck = false;

                OnError(
                    "Atlas result subscribe command rejected(MID0004). Source=" +
                    frame);

                RequestReconnect();
                return;
            }

            AtlasCommandRequest failedRequest = null;

            lock (_commandLock)
            {
                if (_currentCommand != null)
                {
                    failedRequest = _currentCommand;
                    _currentCommand = null;
                }
            }

            if (failedRequest != null)
            {
                RaiseCommandCompleted(
                    failedRequest,
                    false,
                    "Atlas command rejected(MID0004). Source=" + frame);

                TryStartNextCommand();
            }
            else
            {
                OnError(
                    "Atlas command error(MID0004). Source=" +
                    frame);
            }
        }

        private bool EnqueueCommand(
            AtlasCommandType commandType,
            string value,
            string[] commands)
        {
            if (commands == null ||
                commands.Length == 0)
            {
                return false;
            }

            AtlasCommandRequest request =
                new AtlasCommandRequest();

            request.CommandType = commandType;
            request.Value = value ?? string.Empty;
            request.Commands = commands;
            request.CommandIndex = 0;

            lock (_commandLock)
            {
                _commandQueue.Enqueue(request);
            }

            TryStartNextCommand();
            return true;
        }

        private void TryStartNextCommand()
        {
            if (ConnectionState !=
                ToolConnectionState.Communicating)
            {
                return;
            }

            AtlasCommandRequest request = null;

            lock (_commandLock)
            {
                if (_currentCommand != null ||
                    _commandQueue.Count == 0)
                {
                    return;
                }

                _currentCommand =
                    _commandQueue.Dequeue();

                request =
                    _currentCommand;
            }

            SendCurrentCommandStep(request);
        }

        private void SendCurrentCommandStep(
            AtlasCommandRequest request)
        {
            if (request == null)
                return;

            int index =
                request.CommandIndex;

            if (index < 0 ||
                index >= request.Commands.Length)
            {
                FailCurrentCommand(
                    "Atlas command index is invalid.");

                return;
            }

            if (!Send(
                request.Commands[index]))
            {
                FailCurrentCommand(
                    "Atlas command send failed.");

                return;
            }

            lock (_commandLock)
            {
                if (_currentCommand == request)
                    _currentCommand.SentTime = DateTime.Now;
            }
        }

        private static bool TryGetCommandMid(
            AtlasCommandRequest request,
            out string mid)
        {
            mid = string.Empty;

            if (request == null ||
                request.Commands == null)
            {
                return false;
            }

            int index =
                request.CommandIndex;

            if (index < 0 ||
                index >= request.Commands.Length)
            {
                return false;
            }

            string command =
                request.Commands[index];

            return AtlasProtocol.TryGetMid(
                command,
                out mid);
        }

        private void CheckCommandTimeout()
        {
            if (CommandTimeout <= 0)
                return;

            AtlasCommandRequest timedOutRequest = null;

            lock (_commandLock)
            {
                if (_currentCommand == null)
                    return;

                if (_currentCommand.SentTime ==
                    DateTime.MinValue)
                {
                    return;
                }

                double elapsed =
                    (DateTime.Now -
                     _currentCommand.SentTime)
                    .TotalMilliseconds;

                if (elapsed < CommandTimeout)
                    return;

                timedOutRequest =
                    _currentCommand;

                _currentCommand = null;
            }

            RaiseCommandCompleted(
                timedOutRequest,
                false,
                "Atlas command response timeout.");

            FailQueuedCommands(
                "Atlas queued command canceled because the previous command timed out.");

            RequestReconnect();
        }

        private void FailCurrentCommand(
            string message)
        {
            AtlasCommandRequest failedRequest = null;

            lock (_commandLock)
            {
                if (_currentCommand != null)
                {
                    failedRequest =
                        _currentCommand;

                    _currentCommand = null;
                }
            }

            if (failedRequest != null)
            {
                RaiseCommandCompleted(
                    failedRequest,
                    false,
                    message);
            }

            TryStartNextCommand();
        }

        private void FailAllPendingCommands(
            string message)
        {
            AtlasCommandRequest currentRequest = null;

            List<AtlasCommandRequest> queuedRequests =
                new List<AtlasCommandRequest>();

            lock (_commandLock)
            {
                if (_currentCommand != null)
                {
                    currentRequest =
                        _currentCommand;

                    _currentCommand = null;
                }

                while (_commandQueue.Count > 0)
                {
                    queuedRequests.Add(
                        _commandQueue.Dequeue());
                }
            }

            if (currentRequest != null)
            {
                RaiseCommandCompleted(
                    currentRequest,
                    false,
                    message);
            }

            for (int i = 0;
                i < queuedRequests.Count;
                i++)
            {
                RaiseCommandCompleted(
                    queuedRequests[i],
                    false,
                    message);
            }
        }

        private void FailQueuedCommands(
            string message)
        {
            List<AtlasCommandRequest> queuedRequests =
                new List<AtlasCommandRequest>();

            lock (_commandLock)
            {
                while (_commandQueue.Count > 0)
                {
                    queuedRequests.Add(
                        _commandQueue.Dequeue());
                }
            }

            for (int i = 0;
                i < queuedRequests.Count;
                i++)
            {
                RaiseCommandCompleted(
                    queuedRequests[i],
                    false,
                    message);
            }
        }

        private void ProcessTorqueResult(
            string frame)
        {
            TorqueResultData result;
            string errorMessage;

            if (!AtlasProtocol.TryParseTorqueResult(
                frame,
                out result,
                out errorMessage))
            {
                OnError(errorMessage);
                return;
            }

            if (!Send(
                AtlasProtocol.CreateResultAck()))
            {
                RequestReconnect();
                return;
            }

            OnResultReceived(result);
        }

        private void OnResultReceived(
            TorqueResultData result)
        {
            EventHandler<
                ToolResultEventArgs<TorqueResultData>>
                handler = ResultReceived;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolResultEventArgs<TorqueResultData>(
                        result));
            }
        }

        private void RaiseCommandCompleted(
            AtlasCommandRequest request,
            bool success,
            string message)
        {
            if (request == null)
                return;

            EventHandler<AtlasCommandEventArgs>
                handler = CommandCompleted;

            if (handler != null)
            {
                handler(
                    this,
                    new AtlasCommandEventArgs(
                        request.CommandType,
                        request.Value,
                        success,
                        message));
            }
        }
    }
}
