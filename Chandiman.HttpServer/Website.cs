public class Website
{
    public required string Id { get; set; }
    public required string Location { get; set; }
    public required string Path { get; set; }
    public int Port { get; set; }

    public override string ToString()
    {
        return "Id: " + Id + "\nLocation: " + Location
        + "\nPath: " + Path + "\nPort: " + Port;
    }
}
