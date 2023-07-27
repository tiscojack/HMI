using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMI
{
    public enum Status1
    {
        FAILURE,
        DEGRADED,
        MAINTENANCE,
        UNKNOWN,
        OPERATIVE,
        NOSTATUS
    }

    public enum Languages
    {
        IT = 1,
        ES_UK 
    }

    public enum ItemStatus
    {
        NotActive = 0,
        Active
    }

    internal class EnumTypes
    {
    }
}
