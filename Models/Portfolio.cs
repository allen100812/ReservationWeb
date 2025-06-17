using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class Portfolio
    {
        [Required(ErrorMessage = "作品集編號是必填")]
        public int Portfolio_Id { get; set; }

        [Required(ErrorMessage = "作品集分類編號是必填")]
        public int PortfolioGroup_Id { get; set; }

        [Required(ErrorMessage = "作品集標題是必填")]
        public string Portfolio_Title { get; set; }
        public string? Portfolio_Content { get; set; }
        public string? Portfolio_URL { get; set; }
        public bool IsPublished { get; set; }

        public List<PortfolioPhoto> PhotoList { get; set; } = new();
    }
    public class PortfolioGroup
    {
        [Required(ErrorMessage = "作品集群組編號是必填")]
        public int PortfolioGroup_Id { get; set; }
        [Required(ErrorMessage = "作品集群組名稱勢必填")]
        public string PortfolioGroup_Name { get; set; } = string.Empty;
        public string? PortfolioGroup_Content { get; set; }
    }
    public class PortfolioPhoto
    {
        public int Photo_Id { get; set; }
        public int Portfolio_Id { get; set; }
        public byte[] Photo { get; set; } = Array.Empty<byte>();
    }


}
