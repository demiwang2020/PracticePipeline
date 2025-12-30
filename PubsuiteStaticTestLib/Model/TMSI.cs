using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Model
{
    [Table("TMSI")]
    public class TMSI
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int SKU { get; set; }

        [Required]
        public int Arch { get; set; }

        [Required]
        [MaxLength(500)]
        public string MSIPath { get; set; }
    }
}
