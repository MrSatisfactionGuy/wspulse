namespace WsPulse.Models;

public class ServiceInfo
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool IsReachable { get; set;}
    public DateTime LastChecked { get; set;}
}
