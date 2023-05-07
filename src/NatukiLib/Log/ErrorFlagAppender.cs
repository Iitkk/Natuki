namespace NatukiLib
{
    using log4net.Appender;
    using log4net.Core;

    public class ErrorFlagAppender : AppenderSkeleton
    {
        public bool ErrorOccurred { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            ErrorOccurred = true;
        }
    }
}
