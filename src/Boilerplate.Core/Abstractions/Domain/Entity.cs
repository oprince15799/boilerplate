using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Abstractions.Domain
{
    public abstract class Entity : ValueObject
    {
        public long Id { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
        }
    }
}
