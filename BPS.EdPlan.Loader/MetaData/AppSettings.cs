using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdPlanLoaderCore.MetaData
{
    public static class AppSettings
    {
        public static IConfiguration Configuration { get; set; }
    }
}
