using static SC.SUGMA.Log;
namespace SC.SUGMA
{
    public abstract class ComponentBase
    {
        public string Id;

        public virtual void Init(string id)
        {
            Id = id;
        }
        public abstract void Close();

        public abstract void UpdateTick();
    }
}
