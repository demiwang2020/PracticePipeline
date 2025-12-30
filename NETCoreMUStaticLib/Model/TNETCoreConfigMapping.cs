using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TNETCoreConfigMapping")]
    public class TNETCoreConfigMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string PropertyName { get; set; }

        [MaxLength(50)]
        public string MajorRelease { get; set; }

        public int? Arch { get; set; }

        public bool? IsServer { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
