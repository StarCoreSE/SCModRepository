namespace MIG.Shared.SE
{
    public interface Getter<T> {
        T Get();
    }
    
    public class FixedGetter<T> : Getter<T> {
        T t;
        public FixedGetter(T t) {
            this.t = t;
        }
        public T Get() { return t; }
    }
}