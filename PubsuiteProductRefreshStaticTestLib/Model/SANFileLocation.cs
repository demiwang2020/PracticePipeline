using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Model
{
    [Table("SANFileLocation")]
    public class SANFileLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 FileLocationID { get; set; }

        [Required]
        public Int16 ProductID { get; set; }

        [Required]
        [MaxLength(20)]
        public string ProductSPLevel { get; set; }

        [Required]
        public Int16 CPUID { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(2000)]
        public string FileLocation { get; set; }

        [Required]
        public bool Active { get; set; }

        [MaxLength(50)]
        public string PatchType { get; set; }
    }
}
