using System.Collections.Generic;

namespace CarRentalApi.Model
{
    public class PagedResponse<T>
    {
        public int CurrentPageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int? BranchId { get; set; }  // Add this line
        public IEnumerable<T> Items { get; set; }

        public PagedResponse()
        {
            Items = new List<T>();
        }
    }
}
