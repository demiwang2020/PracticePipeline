using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TTestedUpdate")]
    public class TTestedUpdate
    {
        [Key]
        [MaxLength(50)]
        public string UpdateID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        public int Arch { get; set; }

        [Required]
        [MaxLength(10)]
        public string MajorRelease { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReleaseNumber { get; set; }

        [Required]
        [MaxLength(10)]
        public string ReleaseDate { get; set; }

        [Required]
        public bool IsSecurityRelease { get; set; }

        [Required]
        public bool IsServerBundle { get; set; }

        [Required]
        public bool IsAUBundle { get; set; }
    }
}
