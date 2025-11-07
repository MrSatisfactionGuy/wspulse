namespace WsPulse.Models;

public class ServiceInfo
{
    public string Name { get; set; }
    public string Url { get; set; }
    public List<string> Dependencies { get; set; }
    // Env optional
    // Es ist noch nicht zu 100% klar, ob Dienste von Ext nach Int zugreifen.
    public string Environment { get; set; }
    public bool IsReachable { get; set;}
    public DateTime LastChecked { get; set;}
}
