using System.Collections.Concurrent;

namespace Ase.Serialization
{
    public class DeserializerPool
    {
        private readonly ConcurrentStack<Deserializer> deserializers;

        public DeserializerPool()
        {
            this.deserializers = new ConcurrentStack<Deserializer>();
        }

        public Deserializer GetDeserializer(Serializer serializer)
        {
            if (deserializers.TryPop(out var deserializer))
            {
                deserializer.SetSource(serializer);
                return deserializer;
            }

            return new Deserializer(serializer.Data);
        }

        public void ReturnSerializer(Deserializer deserializer)
        {
            if (deserializer == null)
            {
                return;
            }
            
            this.deserializers.Push(deserializer);
        }
    }
    
}