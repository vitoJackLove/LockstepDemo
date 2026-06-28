using System.Collections.Concurrent;

namespace Ase.Serialization
{
    public class SerializerPool
    {
        private readonly ConcurrentStack<Serializer> serializers;

        public SerializerPool()
        {
            this.serializers = new ConcurrentStack<Serializer>();
        }

        public Serializer GetSerializer()
        {
            if (serializers.TryPop(out var serializer))
            {
                serializer.Reset();
                return serializer;
            }

            return new Serializer();
        }

        public void ReturnSerializer(Serializer serializer)
        {
            if (serializer == null)
            {
                return;
            }
            
            this.serializers.Push(serializer);
        }
    }
    
}