namespace MyProject.Domain;

public class SKU
{
    public string Value { get; }

    public SKU(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU не може бути порожнім.");
            
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Z]{4}-\d{4}$"))
            throw new ArgumentException("SKU повинен відповідати формату 'XXXX-0000' (наприклад, PROD-1001).");

        Value = value;
    }
}