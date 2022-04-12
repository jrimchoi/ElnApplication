using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElnApplication.Models
{
    public class Elncm03 : Controller
    {
        public string CODE_ID { get; set; }
        public string CODE { get; set; }
        public string CODE_NM { get; set; }
        public string CODE_DC { get; set; }
    }
}
