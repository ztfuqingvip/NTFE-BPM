using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Taobao.ServiceFramework.Castles;
using Taobao.ServiceFramework.Interfaces;
using Taobao.Workflow.Activities.Client;

namespace Taobao.Workflow.Host.Test
{
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class BaseTest
    {
        protected string _originator = @"taobao-hz\houkun";
        protected string _superior = @"taobao-hz\djl1854";

        protected ILog _log;
        protected Taobao.Workflow.Activities.Client.ITFlowEngine _clientApi;
        protected Taobao.Workflow.Activities.Management.ITFlowEngine _managementApi;
        protected Taobao.Facades.IUserService _userService;

        [TestFixtureSetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Config()
        {
            var c = new Castle.Windsor.WindsorContainer();
            var endpoint = Taobao.ServiceFramework.Configuration
                .Configure()
                .Castle(c)
                .Log4Net(true)
                .Associate(new Uri(System.Configuration.ConfigurationManager.AppSettings["CenterUri"]))
                .Identity(new ServiceFramework.Identity() { Source = "NTFE-BPM", AuthKey = "75DC6B572D1B940E34159DCD7FF26D8D" })
                .Endpoint();
            endpoint.Run();

            this._log = endpoint.Resolve<ILoggerFactory>().Create(this.GetType());
            this._clientApi = c.Resolve<Taobao.Workflow.Activities.Client.ITFlowEngine>();
            this._managementApi = c.Resolve<Taobao.Workflow.Activities.Management.ITFlowEngine>();
            this._userService = c.Resolve<Taobao.Facades.IUserService>();

            //NTFE使用ID作为账号库用户名
            this._originator = this.GetMappedUserId(this._originator).ToString();
            this._superior = this.GetMappedUserId(this._superior).ToString();
        }
        [TestFixtureTearDown]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup]
        public void Cleanup()
        {
            Taobao.ServiceFramework.Configuration.Cleanup();
        }

        protected void Idle()
        {
            this.Idle(10000);
        }
        protected void Idle(int i)
        {
            System.Threading.Thread.Sleep(i);
        }
        //模拟获取服务中心映射用户标识
        protected Guid GetMappedUserId(string wfUserName)
        {
            return this._userService.GetUserByUserName(wfUserName).ID;
        }
        protected void CreateProcessType(string typeName, string workflow, string settings)
        {
            //发布流程
            this._managementApi.CreateProcessType(typeName, workflow, settings, "对引擎客户端进行集成测试的流程", "test");
        }
        protected Process CreateProcess(string typeName, IDictionary<string, string> dict)
        {
            return this._clientApi.NewProcess(typeName
               , "From Engine Client UnitTest at " + DateTime.Now
               , this._originator
               , 0
               , dict);
        }
    }
}