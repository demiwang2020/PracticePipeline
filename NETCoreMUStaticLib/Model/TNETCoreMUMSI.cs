using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TNETCoreMU_MSI")]
    public class TNETCoreMUMSI
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int BundleID { get; set; }

        [Required]
        public int Arch { get; set; }

        [Required]
        [MaxLength(100)]
        public string MSIName { get; set; }

        [MaxLength(100)]
        public string Installer { get; set; }

        [MaxLength(100)]
        public string ProductCode { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}
