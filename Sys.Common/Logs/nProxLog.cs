using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sys.Common.Logs
{
    public class nProxLog
    {
        private Dictionary<string, LogLevel> logsType = new Dictionary<string, LogLevel>();
        private Dictionary<string, NLog.Logger> logNames = new Dictionary<string, NLog.Logger>();
        private readonly string _location = AppDomain.CurrentDomain.BaseDirectory + "nlog.config";

        public nProxLog()
        {
            logsType.Add(LogConstants.LogType.Trace.ToString(), LogLevel.Trace);
            logsType.Add(LogConstants.LogType.Job.ToString(), LogLevel.Information);
            logsType.Add(LogConstants.LogType.Error.ToString(), LogLevel.Error);
            logsType.Add(LogConstants.LogType.Queue.ToString(), LogLevel.Information);

            SetLogMager();
            GenerateNLOG();
        }

        private void SetLogMager()
        {
            foreach (var item in logsType)
            {
                logNames.Add(item.Key, NLog.LogManager.GetLogger(item.Key));
            }
        }

        private void GenerateNLOG()
        {
            //nlogVariable
            string _variable = "";
            string _target = "";
            string _rules = "";
            foreach (var item in logsType)
            {
                _variable = _variable + string.Format(LogConstants.nlogVariable, item.Key);
                _target = _target + string.Format(LogConstants.nlogTarget, item.Key, item.Key.ToLower());
                _rules = _rules + string.Format(LogConstants.rules, item.Value.ToString(), item.Key.ToLower(), item.Key);
            }

            //Replace
            string result = LogConstants.nlogTemplate.Replace("##1##", _variable).Replace("##2##", _target).Replace("##3##", _rules);
            File.WriteAllText(_location, result);
        }

        public void Log(LogConstants.LogType type, string msg)
        {
            if (!logNames.ContainsKey(type.ToString()))
            {
                return;
            }
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}", msg);
                var logEventInfo = new NLog.LogEventInfo()
                {
                    Level = type.ToString().Equals("Error") ? NLog.LogLevel.Error : NLog.LogLevel.Info,
                    Message = sb.ToString()
                };
                logNames[type.ToString()].Log(logEventInfo);
            }
            catch
            {
            }
        }
    }
}