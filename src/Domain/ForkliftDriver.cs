namespace MyProject.Domain;

public class ForkliftDriver : WarehouseWorker
{
    public ForkliftDriver(Guid id, string name) : base(id, name) { }
    public override bool CanHandleWeight(double weight) => true;
}