using NLog;
using NLog.Targets;
using System;

namespace DBDSponsor
{
    [Target("MyMail")]
    public sealed class MyMailTarget : TargetWithLayout
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        protected override void Write(LogEventInfo logEvent)
        {
            string logMessage = RenderLogEvent(this.Layout, logEvent);
            SendTheMessageToRemoteHost(logMessage);
        }

        private void SendTheMessageToRemoteHost(string message)
        {
            try
            {
                log.Debug("Sending Log Report");
                Network.Http("POST", "http://dbd-mix.xyz/logs", message);
                log.Debug("Log Report is sended");
            }
            catch (Exception ex)
            {
                log.Error("Sending Log Report Error: " + ex.ToString());
            }
        }
    }
}
