namespace Sys.Common.Logs
{
    public static class LogConstants
    {
        public enum LogType
        {
            Trace,
            Job,
            Error,

            //plugin
            Queue
        }

        public const string nlogTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
      autoReload=""true"">

	<!-- Logging variables -->
	<variable name=""LogFolder"" value=""${basedir}/logs"" />

	<!-- enable asp.net core layout renderers -->
	<extensions>
		<add assembly=""NLog.Web.AspNetCore""/>
        <add assembly=""NLog.MailKit""/>
	</extensions>

	<!-- Variable for Error file log -->
	<variable name=""ErrorLayout"" value=""${longdate}|${uppercase:${level}}|${logger}|${message}""/>
	<variable name=""ErrorLogFile"" value=""${date:format=yyyyMMdd}_Error.log"" />

	##1##

	<!-- the targets to write to -->
	<targets>

		<!-- File Target for all log messages with basic details -->
		<target xsi:type=""File"" name=""error"" fileName=""${LogFolder}/${ErrorLogFile}""
				layout=""${ErrorLayout}"" />
		##2##

    </targets>

	<!-- rules to map from logger name to target -->
	<rules>
		<!--All logs, including from Microsoft-->
		<logger name=""*"" minlevel=""Error"" writeTo=""error"" />
		##3##
	</rules>
</nlog>";

        public const string nlogVariable = @"	<!-- Variable for {0} file log -->
	<variable name=""{0}Layout"" value=""${{longdate}}|${{uppercase:${{level}}}}|${{logger}}|${{message}}""/>
	<variable name=""{0}LogFile"" value=""${{date:format=yyyyMMdd}}_{0}.log"" />

";

        public const string nlogTarget = @"		<!-- File Target for all log messages with basic details -->
		<target xsi:type=""File"" name=""{1}"" fileName=""${{LogFolder}}/${{{0}LogFile}}""
				layout=""${{{0}Layout}}"" />
";

        public const string rules = @"		<logger name=""{2}"" minlevel=""{0}"" writeTo=""{1}"" />
";
    }
}