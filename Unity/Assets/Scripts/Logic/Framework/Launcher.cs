using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Lockstep.Math;
using Lockstep.Util;
using Lockstep.Game;
using Lockstep.Network;
using NetMsg.Common;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lockstep.Game
{
    [Serializable]
    public class Launcher : ILifeCycle
    {

        public int CurTick => _serviceContainer.GetService<ICommonStateService>().Tick;

        public static Launcher Instance { get; private set; }

        private ServiceContainer _serviceContainer; // 内部存储着所有实现IService的对象 不止一个地方有存储ServiceContainer
        private ManagerContainer _mgrContainer; // 存储所有继承BaseService的对象
        private TimeMachineContainer _timeMachineContainer; // 存储所有实现ITimeMachine的对象
        private IEventRegisterService _registerService; // EventRegisterService, 用于收集函数并且注册成为事件的形式. 缺点是参数必须以class对象的形式传入

        public string RecordPath;
        public int MaxRunTick = int.MaxValue;
        public Msg_G2C_GameStartInfo GameStartInfo;
        public Msg_RepMissFrame FramesInfo;

        public int JumpToTick = 10;

        private SimulatorService _simulatorService = new SimulatorService();
        private NetworkService _networkService = new NetworkService();


        private IConstStateService _constStateService;
        public bool IsRunVideo => _constStateService.IsRunVideo;
        public bool IsVideoMode => _constStateService.IsVideoMode;
        public bool IsClientMode => _constStateService.IsClientMode;

        public object transform;
        private OneThreadSynchronizationContext _syncContext;
        public void DoAwake(IServiceContainer services)
        {
            _syncContext = new OneThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_syncContext);
            Utils.StartServices();
            if (Instance != null)
            {
                Debug.LogError("LifeCycle Error: Awake more than once!!");
                return;
            }

            Instance = this;
            _serviceContainer = services as ServiceContainer;
            _registerService = new EventRegisterService();
            _mgrContainer = new ManagerContainer();
            _timeMachineContainer = new TimeMachineContainer();

            //AutoCreateManagers;
            var svcs = _serviceContainer.GetAllServices();
            foreach (var service in svcs)
            {
                _timeMachineContainer.RegisterTimeMachine(service as ITimeMachine);
                if (service is BaseService baseService)
                { // 只有继承了BaseService的IService会被认为是Manager
                    _mgrContainer.RegisterManager(baseService);
                }
            }

            _serviceContainer.RegisterService(_timeMachineContainer);
            _serviceContainer.RegisterService(_registerService);
        }


        public void DoStart()
        {
            foreach (var mgr in _mgrContainer.AllMgrs)
            {
                mgr.InitReference(_serviceContainer, _mgrContainer); // 对所有的service执行初始化, 让它们都可以互相拿到对象索引
            }

            //bind events
            foreach (var mgr in _mgrContainer.AllMgrs)
            {
                _registerService.RegisterEvent<EEvent, GlobalEventHandler>("OnEvent_", "OnEvent_".Length,
                    EventHelper.AddListener, mgr); // EventRegisterService.RegisterEvent, 用反射把service里面的OnEvent_开头的方法和EEvent里面的类型一一对应, 最后通过EventHelper.AddListener将这些函数作为事件Listener注册到EventHelper里面
            }

            foreach (var mgr in _mgrContainer.AllMgrs)
            {
                mgr.DoAwake(_serviceContainer);
            }

            _DoAwake(_serviceContainer);

            foreach (var mgr in _mgrContainer.AllMgrs)
            {
                mgr.DoStart();
            }

            _DoStart();
        }

        public void _DoAwake(IServiceContainer serviceContainer)
        {
            _simulatorService = serviceContainer.GetService<ISimulatorService>() as SimulatorService;
            _networkService = serviceContainer.GetService<INetworkService>() as NetworkService;
            _constStateService = serviceContainer.GetService<IConstStateService>();
            _constStateService = serviceContainer.GetService<IConstStateService>();

            if (IsVideoMode)
            {
                _constStateService.SnapshotFrameInterval = 20;
                //OpenRecordFile(RecordPath);
            }
        }

        public void _DoStart()
        {
            //_debugService.Trace("Before StartGame _IdCounter" + BaseEntity.IdCounter);
            //if (!IsReplay && !IsClientMode) {
            //    netClient = new NetClient();
            //    netClient.Start();
            //    netClient.Send(new Msg_JoinRoom() {name = Application.dataPath});
            //}
            //else {
            //    StartGame(0, playerServerInfos, localPlayerId);
            //}


            if (IsVideoMode)
            {
                EventHelper.Trigger(EEvent.BorderVideoFrame, FramesInfo); // 触发函数OnEvent_BorderVideoFrame
                EventHelper.Trigger(EEvent.OnGameCreate, GameStartInfo); // OnEvent_OnGameCreate
            }
            else if (IsClientMode)
            {
                GameStartInfo = _serviceContainer.GetService<IGameConfigService>().ClientModeInfo;
                EventHelper.Trigger(EEvent.OnGameCreate, GameStartInfo); // OnEvent_OnGameCreate
                EventHelper.Trigger(EEvent.LevelLoadDone, GameStartInfo); // OnEvent_LevelLoadDone
            }
        }

        public void DoUpdate(float fDeltaTime)
        {
            _syncContext.Update(); // JTAOO: 多线程??? 貌似没用到
            Utils.UpdateServices(); // LTime和CoroutineHelper的Update
            var deltaTime = fDeltaTime.ToLFloat();
            _networkService.DoUpdate(deltaTime); // 这里update网络, 注意网络service是在统一的DoAwake和DoStart里面初始化的
            if (IsVideoMode && IsRunVideo && CurTick < MaxRunTick)
            {
                _simulatorService.RunVideo();
                return;
            }

            if (IsVideoMode && !IsRunVideo)
            {
                _simulatorService.JumpTo(JumpToTick);
            }

            _simulatorService.DoUpdate(fDeltaTime); // 客户端主要的逻辑模块
            // 注意, 每个Service不一定都有update函数
        }

        public void DoDestroy()
        {
            if (Instance == null) return;
            foreach (var mgr in _mgrContainer.AllMgrs)
            {
                mgr.DoDestroy();
            }

            Instance = null;
        }

        public void OnApplicationQuit()
        {
            DoDestroy();
        }
    }
}