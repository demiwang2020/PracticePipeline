using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TNETCoreMU_Bundle")]
    public class TNETCoreMUBundle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Release { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(20)]
        public string ShortName { get; set; }

        [MaxLength(255)]
        public string InstallerNameX86 { get; set; }

        [MaxLength(255)]
        public string InstallerNameX64 { get; set; }

        [MaxLength(255)]
        public string InstallerNameARM64 { get; set; }

        [Required]
        [MaxLength(255)]
        public string InstallerPath { get; set; }

        [MaxLength(255)]
        public string BundleCodeX86 { get; set; }

        [MaxLength(255)]
        public string BundleCodeX64 { get; set; }

        [MaxLength(255)]
        public string BundleCodeARM64 { get; set; }
    }
}
