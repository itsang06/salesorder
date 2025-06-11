using System;
using System.Collections.Generic;

namespace Sys.Common.Models
{
    public class GenericFilter
    {
        public string Property { get; set; }
        public List<string> Values { get; set; }
    }

    public class EcoparamsWithGenericFilter : EcoParameters
    {
        public List<GenericFilter> Filters { get; set; }
    }
}