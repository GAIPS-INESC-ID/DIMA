using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMA_Sim.Model
{
    public enum CharacteristicType
    {
        Implicit,
        Explicit,
    }

    public class Characteristic
    {
        public String name;
        public float weight;
        public CharacteristicType type = CharacteristicType.Explicit;

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other.GetType() != typeof(Characteristic)) return false;

            return ((Characteristic)other).name == name;
        }

        public override int GetHashCode() 
        {
            return name.GetHashCode();
        }
    }
}
