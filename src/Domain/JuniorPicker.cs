namespace MyProject.Domain;

public class JuniorPicker : WarehouseWorker
{
    public JuniorPicker(Guid id, string name) : base(id, name) { }
    public override bool CanHandleWeight(double weight) => weight <= 20.0;
}