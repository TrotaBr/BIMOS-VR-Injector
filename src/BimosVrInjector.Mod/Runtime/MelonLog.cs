using BimosVrInjector.Core.Abstractions;
using MelonLoader;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class MelonLog : ILog
    {
        private readonly MelonLogger.Instance _logger;

        public MelonLog(MelonLogger.Instance logger)
        {
            _logger = logger;
        }

        public void Info(string message) => _logger.Msg(message);
        public void Warn(string message) => _logger.Warning(message);
        public void Error(string message) => _logger.Error(message);
    }
}
