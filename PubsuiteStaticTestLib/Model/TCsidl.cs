using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Model
{
    [Table("TCsidl")]
    public class TCsidl
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Value { get; set; }
    }
}
