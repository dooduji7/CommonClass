using ToolHandler.Core;

namespace ToolHandler.Atlas
{
    public class AtlasPowerFocusMultiController
        : AtlasMultiControllerBase
    {
        public override ToolType ToolType
        {
            get
            {
                return ToolHandler.Core.ToolType.AtlasPowerFocusMulti;
            }
        }

        protected override string ToolDisplayName
        {
            get
            {
                return "Atlas PowerFocus Multi";
            }
        }

        protected override string ResultMid
        {
            get
            {
                return "0101";
            }
        }

        protected override string ResultSubscribeMid
        {
            get
            {
                return "0100";
            }
        }

        public AtlasPowerFocusMultiController(
            ToolOptions options)
            : base(options)
        {
        }

        protected override string CreateResultSubscribe()
        {
            return AtlasPowerFocusMultiProtocol
                .CreateResultSubscribe();
        }

        protected override string CreateResultAck()
        {
            return AtlasPowerFocusMultiProtocol
                .CreateResultAck();
        }

        protected override string CreateKeepAlive()
        {
            return AtlasPowerFocusMultiProtocol
                .CreateKeepAlive();
        }

        protected override bool TryParseTorqueResults(
            string frame,
            out TorqueResultData[] results,
            out string errorMessage)
        {
            return AtlasPowerFocusMultiProtocol
                .TryParseTorqueResults(
                    frame,
                    out results,
                    out errorMessage);
        }
    }
}
