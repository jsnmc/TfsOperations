using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsOperations.Interfaces
{
    public interface IBuildStats
    {
        int TotalBuildFailures { get; set; }
        int TotalProjectFailures { get; set; }
        int TotalBuilds { get; set; }
    }
}
