using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElnApplication.Controllers.Exceptions
{
    public class ProcessException : Exception
    {
        public ProcessException()
        {

        }

        public ProcessException(string message) : base(message)
        {
        }

        public ProcessException(string message, Exception inner)
        : base(message, inner)
        {

        }
    }
}
