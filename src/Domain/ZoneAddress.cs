namespace MyProject.Domain;

public class ZoneAddress
{
    public string Sector { get; } 
    public int Rack { get; }  
    public int Shelf { get; }  

    public ZoneAddress(string sector, int rack, int shelf)
    {
        if (string.IsNullOrWhiteSpace(sector) || sector.Length != 1)
            throw new ArgumentException("Сектор має бути однією літерою.");
        if (rack <= 0 || shelf <= 0)
            throw new ArgumentException("Номер стелажа та полиці має бути більшим за 0.");

        Sector = sector.ToUpper();
        Rack = rack;
        Shelf = shelf;
    }

    public override string ToString() => $"{Sector}-{Rack}-{Shelf}";
}