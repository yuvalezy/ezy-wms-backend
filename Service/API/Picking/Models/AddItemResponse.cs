namespace Service.API.Picking.Models;

public class AddItemResponse {
    public bool Ok             { get; set; }
    public bool ClosedDocument { get; set; }

    public static AddItemResponse OkResponse => new AddItemResponse { Ok = true };
}