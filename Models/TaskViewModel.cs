using System.ComponentModel.DataAnnotations;

namespace test.Models
{
    public class TaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhiệm vụ")]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public string Status { get; set; } = "Pending";

        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

        public int ProjectId { get; set; }

        public int? AssignedTo { get; set; }
    }
}