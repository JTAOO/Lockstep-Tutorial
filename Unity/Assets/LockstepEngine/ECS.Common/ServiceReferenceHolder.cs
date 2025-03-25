namespace Lockstep.Game {

    public class ServiceReferenceHolder {
        protected IServiceContainer _serviceContainer;
        protected IECSFacadeService _ecsFacadeService;


        protected IRandomService _randomService;
        protected ITimeMachineService _timeMachineService;
        protected IConstStateService _constStateService;
        protected IViewService _viewService;
        protected IAudioService _audioService;
        protected IInputService _inputService;
        protected IMap2DService _map2DService;
        protected IResService _resService;
        protected IEffectService _effectService;
        protected IEventRegisterService _eventRegisterService;
        protected IIdService _idService;
        protected ICommonStateService _commonStateService;
        protected IDebugService _debugService;


        protected T GetService<T>() where T : IService{
            return _serviceContainer.GetService<T>();
        }

        // 所有的BaseService都继承ServiceReferenceHolder, 应该是为了像单例一样每个service都能获取到其他的service
        // 要注意InitReference函数的调用时机
        public virtual void InitReference(IServiceContainer serviceContainer,IManagerContainer mgrContainer){
            _serviceContainer = serviceContainer;
            //通用Service
            _ecsFacadeService = serviceContainer.GetService<IECSFacadeService>();
            _randomService = serviceContainer.GetService<IRandomService>();
            _timeMachineService = serviceContainer.GetService<ITimeMachineService>();
            _constStateService = serviceContainer.GetService<IConstStateService>();
            _inputService = serviceContainer.GetService<IInputService>();
            _viewService = serviceContainer.GetService<IViewService>();
            _audioService = serviceContainer.GetService<IAudioService>();
            _map2DService = serviceContainer.GetService<IMap2DService>();
            _resService = serviceContainer.GetService<IResService>();
            _effectService = serviceContainer.GetService<IEffectService>();
            _eventRegisterService = serviceContainer.GetService<IEventRegisterService>();
            _idService = serviceContainer.GetService<IIdService>();
            _commonStateService = serviceContainer.GetService<ICommonStateService>();
            _debugService = serviceContainer.GetService<IDebugService>();

        }
    }

}