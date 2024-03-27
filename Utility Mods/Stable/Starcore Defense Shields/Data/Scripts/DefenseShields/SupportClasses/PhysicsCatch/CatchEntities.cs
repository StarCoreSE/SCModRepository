using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace DefenseShields.Support
{
    public class PhantomSphere : MySafeZone
    {
        private readonly List<long> _entityCompare = new List<long>();
        private readonly List<MyEntity> _entityList = new List<MyEntity>();
        private int _counter = 0;

        public PhantomSphere()
        {
            Closing();
        }

        protected sealed override void Closing()
        {
            base.Closing();
        }

        public List<MyEntity> ReturnEntitySet()
        {
            var isEqual = true;
            var countEqual = m_containedEntities.Count == _entityCompare.Count;
            if (countEqual)
            {
                if (_counter == 9)
                {
                    for (int i = 0; i < _entityCompare.Count; i++)
                    {
                        if (!m_containedEntities.Contains(_entityCompare[i]))
                        {
                            isEqual = false;
                            break;
                        }
                    }
                }
            }
            else isEqual = false;
            if (_counter++ > 9) _counter = 0;

            if (isEqual) return _entityList;
            _entityList.Clear();
            _entityCompare.Clear();
            foreach (var id in m_containedEntities)
            {
                _entityCompare.Add(id);
                _entityList.Add(MyEntities.GetEntityById(id));
            }
            return _entityList;
        }
    }
}
