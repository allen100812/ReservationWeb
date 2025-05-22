using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class Place
    {
        public int? Placeid { get; set; }
        [Required(ErrorMessage = "地點名稱是必須的")]
        public string? Placetitle { get; set; }
        [Required(ErrorMessage = "地點地址名稱是必須的")]
        public string? Placeaddress { get; set; }
        public string? Placeorder { get; set; }
        public int? PlaceSw { get; set; }

        public string? Placepid { get; set; }

        public string? Placemapurl { get; set; }
    }
}
