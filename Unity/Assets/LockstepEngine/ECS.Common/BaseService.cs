using System;
using System.Text;
using Lockstep.Math;

namespace Lockstep.Game {
    // 所有的BaseService都继承ServiceReferenceHolder, 应该是为了像单例一样每个service都能获取到其他的service
    public abstract partial class BaseService : ServiceReferenceHolder, IService, ILifeCycle, ITimeMachine ,IHashCode,IDumpStr{
        public virtual void DoInit(object objParent){}
        public virtual void DoAwake(IServiceContainer services){ }
        public virtual void DoStart(){ }
        public virtual void DoDestroy(){ }
        public virtual void OnApplicationQuit(){ }
        public virtual int GetHash(ref int idx){return 0;}
        public virtual void DumpStr(StringBuilder sb,string prefix){}

        protected BaseService(){
            cmdBuffer = new CommandBuffer();
            cmdBuffer.Init(this, GetRollbackFunc());
        }


        protected ICommandBuffer cmdBuffer;

        protected virtual FuncUndoCommands GetRollbackFunc(){
            return null;
        }

        public int CurTick => _commonStateService.Tick;

        public virtual void Backup(int tick){ }

        public virtual void RollbackTo(int tick){
            cmdBuffer?.Jump(CurTick, tick);
        }

        public virtual void Clean(int maxVerifiedTick){
            cmdBuffer?.Clean(maxVerifiedTick);
        }
    }
}