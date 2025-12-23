namespace WsPulse.Config;

public class PollingConfig
{
    public int IntervalSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 5;
    public int MaxParallel { get; set; } = 10;
}
