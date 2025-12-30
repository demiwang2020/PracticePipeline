using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Model
{
    [Table("TPublishChannel")]
    public class TPublishChannel
    {
       
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int OS { get; set; }

        [Required]
        public int SKU { get; set; }

        [Required]
        public bool MTPChannel { get; set; }

        [Required]
        public bool MediaFlag { get; set; }
        public string MTPdeliveryRecipients { get; set; }

    }
}
