using ToolHandler.Core;

namespace ToolHandler.Atlas
{
    public class AtlasPowerMacMultiController
        : AtlasMultiControllerBase
    {
        public override ToolType ToolType
        {
            get
            {
                return ToolHandler.Core.ToolType.AtlasPowerMacMulti;
            }
        }

        protected override string ToolDisplayName
        {
            get
            {
                return "Atlas PowerMac Multi";
            }
        }

        protected override string ResultMid
        {
            get
            {
                return "0106";
            }
        }

        protected override string ResultSubscribeMid
        {
            get
            {
                return "0105";
            }
        }

        public AtlasPowerMacMultiController(
            ToolOptions options)
            : base(options)
        {
        }

        protected override string CreateResultSubscribe()
        {
            return AtlasPowerMacMultiProtocol
                .CreateResultSubscribe();
        }

        protected override string CreateResultAck()
        {
            return AtlasPowerMacMultiProtocol
                .CreateResultAck();
        }

        protected override string CreateKeepAlive()
        {
            return AtlasPowerMacMultiProtocol
                .CreateKeepAlive();
        }

        protected override bool TryParseTorqueResults(
            string frame,
            out TorqueResultData[] results,
            out string errorMessage)
        {
            return AtlasPowerMacMultiProtocol
                .TryParseTorqueResults(
                    frame,
                    out results,
                    out errorMessage);
        }
    }
}
