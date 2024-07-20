namespace SC.SUGMA
{
    public abstract class ComponentBase
    {
        public string ComponentId;

        public virtual void Init(string id)
        {
            ComponentId = id;
        }

        public abstract void Close();

        public abstract void UpdateTick();
    }
}