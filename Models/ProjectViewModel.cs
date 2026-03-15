using System.ComponentModel.DataAnnotations;

namespace test.Models
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dự án")]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        public IFormFile? ImageFile { get; set; }
        public string? ExistingImageUrl { get; set; }
    }
}