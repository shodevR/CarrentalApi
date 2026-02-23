namespace CarRentalApi.Model
{
	public class ResponseModel
	{
		public string Status { get; set; }

		public object Data { get; set; }

		public string Message { get; set; }
	}

	public enum StatusEnums
    {
        success,
        error
    }
}
