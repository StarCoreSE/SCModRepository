using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.CSharp;
using VRage.ModAPI;

namespace MIG.Shared.SE {
    public static class FrameExecutor {
        private static int frame = 0;
        public static int currentFrame { get {  return frame; } }
        
        private static readonly List<Action1<long>> onEachFrameLogic = new List<Action1<long>>();
        private static readonly List<Action1<long>> addOnEachFrameLogic = new List<Action1<long>>();
        private static readonly List<Action1<long>> removeOnEachFrameLogic = new List<Action1<long>>();
        private static bool needRemoveFrameLogic = false;
        
        public static void Update() {
            try {
                foreach (var x in onEachFrameLogic) {
                    x.run(frame);
                }

                onEachFrameLogic.AddList(addOnEachFrameLogic);
                foreach (var x in removeOnEachFrameLogic)
                {
                    onEachFrameLogic.Remove(x);
                }
                addOnEachFrameLogic.Clear();
                removeOnEachFrameLogic.Clear();
                frame++;
            } catch (Exception e) {
                Log.ChatError("FrameExecutor", e);
            }
        } 
        
        public static void addFrameLogic(Action1<long> action) {
            Log.ChatError("TotalFrameLogics:" + addOnEachFrameLogic.Count + " " + onEachFrameLogic.Count);
            addOnEachFrameLogic.Add(action);
        }
        
        public static ActionWrapper addFrameLogic(Action<long> action) {
            Log.ChatError("TotalFrameLogics:" + addOnEachFrameLogic.Count + " " + onEachFrameLogic.Count);
            ActionWrapper wrapper = new ActionWrapper(action);
            addOnEachFrameLogic.Add(wrapper);
            return wrapper;
        }

        public static BlockWrapperAction addFrameLogic(Timer timer, IMyEntity entity, Action<long> action)
        {
            return addFrameLogic(timer, entity, new ActionWrapper(action));
        }
        
        public static BlockWrapperAction addFrameLogic(Timer timer, IMyEntity entity, Action1<long> action)
        {
            if (entity == null)
            {
                Log.ChatError("addFrameLogic:Entity is null");
                return new BlockWrapperAction(entity, timer, action);
            }
            BlockWrapperAction wrapper = new BlockWrapperAction(entity, timer, action);
            addOnEachFrameLogic.Add(wrapper);
            return wrapper;
        }
        
        public static void removeFrameLogic(Action1<long> action) {
            removeOnEachFrameLogic.Add(action);
        }

        public static void addDelayedLogic(long frames, Action1<long> action) {
            addOnEachFrameLogic.Add(new DelayerAction(frames, action));
        }

        public static DelayerAction addDelayedLogic(long frames, Action<long> action)
        {
            Log.ChatError("TotalFrameLogics:" + addOnEachFrameLogic.Count + " " + onEachFrameLogic.Count);
            var da = new DelayerAction(frames, new ActionWrapper(action));
            addOnEachFrameLogic.Add(da);
            return da;
        }

        public class ActionWrapper : Action1<long>
        {
            Action<long> action;
            public ActionWrapper (Action<long> action)
            {
                this.action = action;
            }

            public void run(long t)
            {
                action(t);
            }

            public void Unsub()
            {
                FrameExecutor.removeFrameLogic(this);
            }
        }
        

        public class BlockWrapperAction : Action1<long> {
            private Timer timer;
            private Action1<long> action;
            private IMyEntity entity;
            public BlockWrapperAction(IMyEntity entity, Timer timer, Action1<long> action) {
                this.timer = timer;
                this.action = action;
                this.entity = entity;
                if (entity == null)
                {
                    
                }
            }

            public void Cancel()
            {
                FrameExecutor.removeFrameLogic(this); 
            }
            
            public void RunNow()
            {
                action.run(-1);
            }

            public void run(long k) {
                if (timer.tick())
                {
                    if (entity.MarkedForClose || entity.Closed)
                    {
                        Cancel();
                        return;
                    }
                    action.run(k);
                }
            }
        }
        
        
        public class DelayerAction : Action1<long> {
            private long timer;
            private Action1<long> action;
            public DelayerAction(long timer, Action1<long> action) {
                this.timer = timer;
                this.action = action;
            }

            public void Cancel()
            {
                FrameExecutor.removeFrameLogic(this); 
            }
            
            public void RunNow()
            {
                action.run(-1);
            }

            public void run(long k) {
                if (timer > 0) {
                    timer--; return;
                }
                FrameExecutor.removeFrameLogic(this); 
                action.run(k);
            }
        }
    }
}